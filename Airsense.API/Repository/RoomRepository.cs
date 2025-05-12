using System.Data;
using Airsense.API.Models.Dto.Room;
using Airsense.API.Models.Dto.Sensor;
using Airsense.API.Models.Entity;
using Dapper;

namespace Airsense.API.Repository;

public class RoomRepository(IDbConnection connection) : IRoomRepository
{
    public async Task<ICollection<RoomDto>> GetAsync(int envId, int skip, int count)
    {
        const string sql = """
                           WITH latest_sensor_data AS (
                               SELECT DISTINCT ON (sd.sensor_id, sd.parameter_id)
                                   sd.sensor_id,
                                   sd.parameter_id,
                                   sd.value,
                                   sd.timestamp,
                                   p.name AS parameter,
                                   p.unit,
                                   p.min_value,
                                   p.max_value
                               FROM sensor_data sd
                                        JOIN parameters p ON sd.parameter_id = p.id
                               ORDER BY sd.sensor_id, sd.parameter_id DESC, sd.timestamp DESC
                           ),
                                latest_device_data AS (
                                    SELECT DISTINCT ON (dd.device_id)
                                        dd.device_id,
                                        dd.timestamp AS applied_at,
                                        dd.value AS DeviceSpeed
                                    FROM device_data dd
                                    ORDER BY dd.device_id, dd.timestamp DESC, dd.value DESC
                                )
                           SELECT
                               r.id AS Id,
                               r.name AS Name,
                               MAX(ldd.DeviceSpeed) AS DeviceSpeed,
                               lsd.parameter AS ParamKey,
                               lsd.unit AS ParamUnit,
                               lsd.min_value AS ParamMinValue,
                               lsd.max_value AS ParamMaxValue,
                               AVG(lsd.value) AS ParamValue
                           FROM rooms r
                                    LEFT JOIN devices d ON r.id = d.room_id
                                    LEFT JOIN latest_device_data ldd ON d.id = ldd.device_id AND ldd.applied_at > NOW() - INTERVAL '1 minute'
                                    LEFT JOIN sensors s ON r.id = s.room_id
                                    LEFT JOIN latest_sensor_data lsd ON s.id = lsd.sensor_id AND lsd.timestamp > NOW() - INTERVAL '1 minute'
                           WHERE r.environment_id = @envId
                           GROUP BY r.id, r.name, lsd.parameter, lsd.unit, lsd.min_value, lsd.max_value
                           ORDER BY r.id
                           """;

        var roomData = await connection.QueryAsync<RoomRawDto>(sql, new { envId });

        var rooms = roomData
            .GroupBy(r => new { r.Id, r.Name })
            .Select(g => new RoomDto
            {
                Id = g.Key.Id,
                Name = g.Key.Name,
                DeviceSpeed = g.Max(x => x.DeviceSpeed),
                Parameters = g
                    .Where(x => x.ParamKey is not null)
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

        return rooms.ToList();
    }
    
    public async Task<int> CountAsync(int envId)
    {
        const string sql = "SELECT COUNT(*) FROM rooms r WHERE r.environment_id = @envId";
        var count = await connection.QuerySingleAsync<int>(sql, new { envId });
        return count;
    }
    
    public async Task<Room> CreateAsync(Room room)
    {
        const string sql = """
                           INSERT INTO rooms (name, environment_id) 
                           VALUES (@Name, @EnvironmentId) 
                           RETURNING 
                                 id AS Id,
                                 name AS Name,
                                 environment_id AS EnvironmentId
                           """;
        var result = await connection.QuerySingleAsync<Room>(sql, room);
        return result;
    }
    
    public async Task<RoomDto?> GetByIdAsync(int roomId)
    {
        const string sql = """
                           WITH latest_sensor_data AS (
                               SELECT DISTINCT ON (sd.sensor_id, sd.parameter_id)
                                   sd.sensor_id,
                                   sd.parameter_id,
                                   sd.value,
                                   sd.timestamp,
                                   p.name AS parameter,
                                   p.unit,
                                   p.min_value,
                                   p.max_value
                               FROM sensor_data sd
                               JOIN parameters p ON sd.parameter_id = p.id
                               ORDER BY sd.sensor_id, sd.parameter_id DESC, sd.timestamp DESC
                           ),
                           latest_device_data AS (
                               SELECT DISTINCT ON (dd.device_id)
                                   dd.device_id,
                                   dd.applied_at,
                                   dd.value AS DeviceSpeed
                               FROM device_data dd
                               WHERE dd.applied_at IS NOT NULL
                               ORDER BY dd.device_id, dd.applied_at DESC, dd.value DESC
                           )
                           SELECT
                               r.id AS Id,
                               r.name AS Name,
                               MAX(ldd.DeviceSpeed) AS DeviceSpeed,
                               lsd.parameter AS ParamKey,
                               lsd.unit AS ParamUnit,
                               lsd.min_value AS ParamMinValue,
                               lsd.max_value AS ParamMaxValue,
                               AVG(lsd.value) AS ParamValue
                           FROM rooms r
                                    LEFT JOIN devices d ON r.id = d.room_id
                                    LEFT JOIN latest_device_data ldd ON d.id = ldd.device_id AND ldd.applied_at > NOW() - INTERVAL '1 minute'
                                    LEFT JOIN sensors s ON r.id = s.room_id
                                    LEFT JOIN latest_sensor_data lsd ON s.id = lsd.sensor_id AND lsd.timestamp > NOW() - INTERVAL '1 minute'
                           WHERE r.id = @roomId
                           GROUP BY r.id, r.name, lsd.parameter, lsd.unit, lsd.min_value, lsd.max_value
                           ORDER BY r.id
                           """;
        var roomData = await connection.QueryAsync<RoomRawDto>(sql, new { roomId });

        var room = roomData
            .GroupBy(r => new { r.Id, r.Name })
            .Select(g => new RoomDto
            {
                Id = g.Key.Id,
                Name = g.Key.Name,
                DeviceSpeed = g.Average(x => x.DeviceSpeed),
                Parameters = g
                    .Where(x => x.ParamKey is not null)
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
            });

        return room.FirstOrDefault();
    }
    
    public async Task UpdateAsync(int roomId, string name)
    {
        const string sql = "UPDATE rooms SET name = @name WHERE id = @roomId";
        await connection.ExecuteAsync(sql, new { name, roomId });
    }
    
    public async Task<bool> IsExistsAsync(int roomId, int envId)
    {
        const string sql = "SELECT 1 FROM rooms r WHERE r.id = @roomId AND r.environment_id = @envId";
        var result = await connection.QueryAsync(sql, new { roomId, envId });
        return result.SingleOrDefault() != null;
    }
    
    public async Task DeleteAsync(int roomId)
    {
        const string sql = "DELETE FROM rooms WHERE id = @roomId";
        await connection.ExecuteAsync(sql, new { roomId });
    }
    
    public async Task<bool> IsHasAccessAsync(int userId, int roomId)
    {
        const string sql = """
                           SELECT 1 
                           FROM rooms r
                           JOIN environments e ON r.environment_id = e.id
                           JOIN environment_members em ON e.id = em.environment_id
                           WHERE r.id = @roomId AND em.member_id = @userId AND em.role <> 'user'  
                           """;
        var result = await connection.QueryAsync(sql, new { userId, roomId });
        return result.SingleOrDefault() != null;
    }
    
    public async Task<ICollection<ParameterDto>> GetAvailableTypesAsync(int roomId)
    {
        const string sql = """
                           SELECT DISTINCT ON (p.name)
                               p.name AS Name,
                               p.unit AS Unit,
                               p.min_value AS MinValue,   
                               p.max_value AS MaxValue
                           FROM sensors s
                           JOIN sensor_types t ON s.type_id = t.id
                           JOIN sensor_type_parameters tp ON t.id = tp.type_id
                           JOIN parameters p ON tp.parameter_id = p.id
                           WHERE s.room_id = @roomId
                           """;
        
        var types = await connection.QueryAsync<ParameterDto>(sql, new { roomId });
        return types.ToList();
    }
}