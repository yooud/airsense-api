using System.Data;
using Airsense.API.Models.Dto.Room;
using Airsense.API.Models.Dto.Sensor;
using Airsense.API.Models.Entity;
using Dapper;

namespace Airsense.API.Repository;

public class SensorRepository(IDbConnection connection) : ISensorRepository
{
    public async Task<ICollection<SensorDto>> GetAsync(int roomId, int count, int skip)
    {
        const string sql = """
                           SELECT
                               s.id AS Id,
                               s.serial_number AS SerialNumber,
                               t.name AS TypeName,
                               sp.name AS SensorParameter,
                               dp.name AS ParamKey,
                               sd.value AS ParamValue,
                               dp.unit AS ParamUnit,
                               dp.min_value AS ParamMinValue,
                               dp.max_value AS ParamMaxValue
                           FROM sensors s
                           JOIN sensor_types t ON s.type_id = t.id
                           JOIN sensor_type_parameters tp ON tp.type_id = t.id
                           JOIN parameters sp ON tp.parameter_id = sp.id
                           LEFT JOIN (
                               SELECT DISTINCT ON (sensor_id, parameter_id)
                                   sensor_id, parameter_id, value, timestamp
                               FROM sensor_data
                               ORDER BY sensor_id, parameter_id, timestamp DESC
                           ) sd ON s.id = sd.sensor_id AND sd.timestamp > NOW() - INTERVAL '1 minute'
                           LEFT JOIN parameters dp ON dp.id = sd.parameter_id
                           WHERE s.room_id = @roomId
                           ORDER BY s.id
                           """;

        var sensorData = await connection.QueryAsync<SensorRawDto>(sql, new { roomId });

        var sensors = sensorData
            .GroupBy(s => new { s.Id, s.SerialNumber, s.TypeName })
            .Select(g => new SensorDto
            {
                Id = g.Key.Id,
                TypeName = g.Key.TypeName,
                SerialNumber = g.Key.SerialNumber,
                Types = g.Select(s => s.SensorParameter).ToList(),
                Parameters = g
                    .Where(x => x.ParamKey is not null)
                    .DistinctBy(x => x.ParamKey)
                    .Select(
                        x => new ParameterDto
                        {
                            Name = x.ParamKey,
                            Value = x.ParamValue.GetValueOrDefault(),
                            MinValue = x.ParamMinValue.GetValueOrDefault(),
                            MaxValue = x.ParamMaxValue.GetValueOrDefault(),
                            Unit = x.ParamUnit
                        }
                    ).ToList()
            })
            .Select(room =>
            {
                if (room.Parameters == null || room.Parameters.Count == 0)
                    room.Parameters = null;
                return room;
            })
            .Skip(skip)
            .Take(count);

        return sensors.ToList();
    }

    public async Task<int> CountAsync(int roomId)
    {
        const string sql = "SELECT COUNT(*) FROM sensors s WHERE s.room_id = @roomId";
        var count = await connection.QuerySingleAsync<int>(sql, new { roomId });
        return count;
    }

    public async Task<Sensor?> GetByIdAsync(int sensorId)
    {
        const string sql = """
                           SELECT 
                               s.id AS Id, 
                               s.serial_number AS SerialNumber,
                               s.room_id AS RoomId,
                               s.type_id AS TypeId
                           FROM sensors s
                           WHERE s.id = @sensorId
                           """;
        var sensor = await connection.QueryFirstOrDefaultAsync<Sensor>(sql, new { sensorId });
        return sensor;
    }

    public async Task<Sensor?> GetBySerialNumberAsync(string serialNumber)
    {
        const string sql = """
                           SELECT 
                               s.id AS Id, 
                               s.serial_number AS SerialNumber,
                               s.room_id AS RoomId,
                               s.type_id AS TypeId,
                               s.secret AS Secret
                           FROM sensors s
                           WHERE s.serial_number = @serialNumber
                           """;
        var sensor = await connection.QueryFirstOrDefaultAsync<Sensor>(sql, new { serialNumber });
        return sensor;
    }

    public async Task UpdateRoomAsync(int sensorId, int roomId)
    {
        const string sql = "UPDATE sensors SET room_id = @roomId WHERE id = @sensorId";
        await connection.ExecuteAsync(sql, new { sensorId, roomId });
    }

    public async Task DeleteRoomAsync(int sensorId)
    {
        const string sql = "UPDATE sensors SET room_id = NULL WHERE id = @sensorId";
        await connection.ExecuteAsync(sql, new { sensorId });
    }

    public async Task AddDataAsync(int sensorId, string parameter, SensorDataDto data)
    {
        const string sql = """
                           INSERT INTO sensor_data (sensor_id, parameter_id, value, sent_at)
                           SELECT @sensorId, p.id, @value, to_timestamp(@sentAt)
                           FROM parameters p
                           WHERE p.name = @parameter
                           """;
        await connection.ExecuteAsync(sql, new { sensorId, parameter, data.Value, data.SentAt });
    }

    public async Task<bool> IsExistsBySentAt(int sensorId, long sentAt)
    {
        const string sql = """
                           SELECT 1
                           FROM sensor_data sd
                           WHERE sd.sensor_id = @sensorId AND
                               sent_at = to_timestamp(@sentAt)
                           """;

        var result = await connection.QueryAsync(sql, new { sensorId, sentAt });
        return result.SingleOrDefault() != null;
    }

    public async Task<ICollection<string>> GetTypesAsync(int sensorId)
    {
        const string sql = """
                           SELECT p.name
                           FROM sensors s
                           JOIN sensor_types t ON s.type_id = t.id
                           JOIN sensor_type_parameters tp ON tp.type_id = t.id
                           JOIN parameters p ON tp.parameter_id = p.id
                           WHERE s.id = @sensorId
                           """;
        var types = await connection.QueryAsync<string>(sql, new { sensorId });
        return types.ToList();
    }

    public async Task<ICollection<HistoryDeviceDto>> GetRoomHistoryAsync(
        int roomId,
        string parameter,
        DateTime fromDate,
        DateTime toDate,
        HistoryDto.HistoryInterval interval
    )
    {
        string intervalSql;
        switch (interval)
        {
            case HistoryDto.HistoryInterval.Minute:
                intervalSql = "date_trunc('minute', sd.timestamp)";
                break;
            case HistoryDto.HistoryInterval.Day:
                intervalSql = "date_trunc('day', sd.timestamp)";
                break;
            case HistoryDto.HistoryInterval.Hour:
            default:
                intervalSql = "date_trunc('hour', sd.timestamp)";
                break;
        }

        var sql = $"""
                   SELECT 
                       s.id AS Id,
                       s.serial_number AS SerialNumber,
                       t.name AS TypeName,
                       EXTRACT(EPOCH FROM {intervalSql}) AS Timestamp,
                       AVG(sd.value) AS Value
                   FROM sensors s
                   JOIN sensor_types t ON s.type_id = t.id
                   LEFT JOIN sensor_data sd ON sd.sensor_id = s.id
                   LEFT JOIN parameters dp ON sd.parameter_id = dp.id
                   WHERE s.room_id = @roomId
                   AND dp.name = @parameter
                   AND sd.timestamp BETWEEN @fromDate AND @toDate
                   GROUP BY s.id, {intervalSql}, t.name
                   ORDER BY s.id, {intervalSql}
                   """;

        var historyData = await connection.QueryAsync<HistoryRawDto>(sql, new { roomId, parameter, fromDate, toDate });

        var history = historyData
            .GroupBy(s => new { s.Id, s.TypeName, s.SerialNumber })
            .Select(g => new HistoryDeviceDto
            {
                Id = g.Key.Id,
                TypeName = g.Key.TypeName,
                SerialNumber = g.Key.SerialNumber,
                History = g.Where(x => x.Timestamp is not null && x.Value is not null).Select(x =>
                    new HistoryDeviceDataDto
                    {
                        Timestamp = x.Timestamp!.Value,
                        Value = x.Value!.Value
                    }).ToList()
            });

        return history.ToList();
    }

    public async Task<HistoryDeviceDto?> GetSensorHistoryAsync(
        int sensorId,
        string parameter,
        DateTime fromDate,
        DateTime toDate,
        HistoryDto.HistoryInterval interval
    )
    {
        string intervalSql;
        switch (interval)
        {
            case HistoryDto.HistoryInterval.Minute:
                intervalSql = "date_trunc('minute', sd.timestamp)";
                break;
            case HistoryDto.HistoryInterval.Day:
                intervalSql = "date_trunc('day', sd.timestamp)";
                break;
            case HistoryDto.HistoryInterval.Hour:
            default:
                intervalSql = "date_trunc('hour', sd.timestamp)";
                break;
        }

        var sql = $"""
                   SELECT 
                       s.id AS Id,
                       s.serial_number AS SerialNumber,
                       t.name AS TypeName,
                       EXTRACT(EPOCH FROM {intervalSql}) AS Timestamp,
                       AVG(sd.value) AS Value
                   FROM sensors s
                   JOIN sensor_types t ON s.type_id = t.id 
                   LEFT JOIN sensor_data sd ON sd.sensor_id = s.id
                   LEFT JOIN parameters dp ON sd.parameter_id = dp.id
                   WHERE s.id = @sensorId
                   AND dp.name = @parameter
                   AND sd.timestamp BETWEEN @fromDate AND @toDate
                   GROUP BY s.id, {intervalSql}, t.name
                   ORDER BY s.id, {intervalSql}
                   """;

        var historyData =
            await connection.QueryAsync<HistoryRawDto>(sql, new { sensorId, parameter, fromDate, toDate });

        var history = historyData
            .GroupBy(s => new { s.Id, s.TypeName, s.SerialNumber })
            .Select(g => new HistoryDeviceDto
            {
                Id = g.Key.Id,
                TypeName = g.Key.TypeName,
                SerialNumber = g.Key.SerialNumber,
                History = g.Where(x => x.Timestamp is not null && x.Value is not null).Select(x =>
                    new HistoryDeviceDataDto
                    {
                        Timestamp = x.Timestamp!.Value,
                        Value = x.Value!.Value
                    }).ToList()
            });

        return history.FirstOrDefault();
    }
}