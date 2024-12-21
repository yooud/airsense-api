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
                               t.parameters AS Types,
                               sd.parameter AS ParamKey,
                               sd.value AS ParamValue
                           FROM sensors s
                           JOIN sensor_types t ON s.type_id = t.id
                           LEFT JOIN (
                               SELECT DISTINCT ON (sensor_id, parameter) 
                                   sensor_id, parameter, value, timestamp
                               FROM sensor_data
                               ORDER BY sensor_id, parameter, timestamp DESC
                           ) sd ON s.id = sd.sensor_id AND sd.timestamp > NOW() - INTERVAL '1 minute'
                           WHERE s.room_id = @roomId
                           """;
        
        var sensorData = await connection.QueryAsync<SensorRawDto>(sql, new { roomId });

        var sensors = sensorData
            .GroupBy(s => new { s.Id, s.SerialNumber, s.Types })
            .Select(g => new SensorDto
            {
                Id = g.Key.Id,
                SerialNumber = g.Key.SerialNumber,
                Types = g.Key.Types.Split(", ").Select(p => p.Trim()).Distinct().ToList(),
                Params = g
                    .Where(x => x.ParamKey != null)
                    .ToDictionary(x => x.ParamKey, x => x.ParamValue)
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
                               s.type_id AS TypeId
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
    
    public async Task AddDataAsync(int sensorId, SensorDataDto data)
    {
        const string sql = "INSERT INTO sensor_data (sensor_id, parameter, value) VALUES (@sensorId, @parameter, @value)";
        await connection.ExecuteAsync(sql, new { sensorId, data.Parameter, data.Value });
    }
    
    public async Task<ICollection<string>> GetTypesAsync(int sensorId)
    {
        const string sql = "SELECT parameters FROM sensor_types WHERE id = (SELECT type_id FROM sensors WHERE id = @sensorId)";
        var types = await connection.QuerySingleAsync<string>(sql, new { sensorId });
        return types.Split(", ").Select(p => p.Trim()).ToList();
    }
    
    public async Task<ICollection<HistoryDeviceDto>> GetRoomHistoryAsync(int roomId, string parameter, DateTime fromDate, DateTime toDate, string interval)
    {
        switch (interval.ToLower())
        {
            case "minute":
                interval = "date_trunc('minute', sd.timestamp)";
                break;
            case "day":
                interval = "date_trunc('day', sd.timestamp)";
                break;
            case "hour":
            default:
                interval = "date_trunc('hour', sd.timestamp)";
                break;
        }

        var sql = $"""
                   SELECT 
                       s.id AS Id,
                       s.serial_number AS SerialNumber,
                       EXTRACT(EPOCH FROM {interval}) AS Timestamp,
                       AVG(sd.value) AS Value
                   FROM sensors s
                   LEFT JOIN sensor_data sd ON sd.sensor_id = s.id
                   WHERE s.room_id = @roomId
                   AND sd.parameter = @parameter
                   AND sd.timestamp BETWEEN @fromDate AND @toDate
                   GROUP BY s.id, {interval}
                   ORDER BY s.id, {interval}
                   """;

        var historyData = await connection.QueryAsync<HistoryRawDto>(sql, new { roomId, parameter, fromDate, toDate });

        var history = historyData
            .GroupBy(s => new { s.Id, s.SerialNumber })
            .Select(g => new HistoryDeviceDto
            {
                Id = g.Key.Id,
                SerialNumber = g.Key.SerialNumber,
                History = g.Where(x => x.Timestamp is not null && x.Value is not null).Select(x => new HistoryDeviceDataDto
                {
                    Timestamp = x.Timestamp!.Value,
                    Value = x.Value!.Value
                }).ToList()
            });

        return history.ToList();
    }

    public async Task<HistoryDeviceDto> GetSensorHistoryAsync(int sensorId, string parameter, DateTime fromDate, DateTime toDate, string interval)
    {
        switch (interval.ToLower())
        {
            case "minute":
                interval = "date_trunc('minute', sd.timestamp)";
                break;
            case "day":
                interval = "date_trunc('day', sd.timestamp)";
                break;
            case "hour":
            default:
                interval = "date_trunc('hour', sd.timestamp)";
                break;
        }

        var sql = $"""
                   SELECT 
                       s.id AS Id,
                       s.serial_number AS SerialNumber,
                       EXTRACT(EPOCH FROM {interval}) AS Timestamp,
                       AVG(sd.value) AS Value
                   FROM sensors s
                   LEFT JOIN sensor_data sd ON sd.sensor_id = s.id
                   WHERE s.id = @sensorId
                   AND sd.parameter = @parameter
                   AND sd.timestamp BETWEEN @fromDate AND @toDate
                   GROUP BY s.id, {interval}
                   ORDER BY s.id, {interval}
                   """;

        var historyData = await connection.QueryAsync<HistoryRawDto>(sql, new { sensorId, parameter, fromDate, toDate });

        var history = historyData
            .GroupBy(s => new { s.Id, s.SerialNumber })
            .Select(g => new HistoryDeviceDto
            {
                Id = g.Key.Id,
                SerialNumber = g.Key.SerialNumber,
                History = g.Where(x => x.Timestamp is not null && x.Value is not null).Select(x => new HistoryDeviceDataDto
                {
                    Timestamp = x.Timestamp!.Value,
                    Value = x.Value!.Value
                }).ToList()
            });

        return history.First();
    }
}