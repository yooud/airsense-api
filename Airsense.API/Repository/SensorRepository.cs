using System.Data;
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
                                   sensor_id, parameter, value 
                               FROM sensor_data
                               ORDER BY timestamp DESC
                           ) sd ON s.id = sd.sensor_id
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
                               s.serial_number AS RoomId,
                               s.type_id AS TypeId
                           FROM sensors s
                           WHERE s.id = @sensorId
                           """;
        var sensor = await connection.QueryFirstOrDefaultAsync(sql, new { sensorId });
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
        var sensor = await connection.QueryFirstOrDefaultAsync(sql, new { serialNumber });
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
}