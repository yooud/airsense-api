using System.Data;
using Airsense.API.Models.Dto.Room;
using Airsense.API.Models.Entity;
using Dapper;

namespace Airsense.API.Repository;

public class RoomRepository(IDbConnection connection) : IRoomRepository
{
    public async Task<ICollection<RoomDto>> GetAsync(int envId, int count, int skip)
    {
        const string sql = """
                           SELECT 
                               r.id AS Id,
                               r.name AS Name,
                               AVG(dd.value) AS DeviceSpeed,
                               sd.parameter AS ParamKey,
                               AVG(sd.value) AS ParamValue
                           FROM rooms r
                           LEFT JOIN devices d ON r.id = d.room_id
                           LEFT JOIN device_data dd ON d.id = dd.device_id
                           LEFT JOIN sensors s ON r.id = s.room_id
                           LEFT JOIN sensor_data sd ON s.id = sd.sensor_id
                           WHERE r.environment_id = @envId
                           GROUP BY r.id, r.name, sd.parameter
                           """;

        var roomData = await connection.QueryAsync<RoomRawDto>(sql, new { envId });

        var rooms = roomData
            .GroupBy(r => new { r.Id, r.Name, r.DeviceSpeed })
            .Select(g => new RoomDto
            {
                Id = g.Key.Id,
                Name = g.Key.Name,
                DeviceSpeed = g.Key.DeviceSpeed,
                Params = g
                    .Where(x => x.ParamKey != null)
                    .ToDictionary(x => x.ParamKey, x => x.ParamValue)
            }).Skip(skip)
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
                           SELECT 
                               r.id AS Id,
                               r.name AS Name,
                               AVG(dd.value) AS DeviceSpeed,
                               sd.parameter AS ParamKey,
                               AVG(sd.value) AS ParamValue
                           FROM rooms r
                           LEFT JOIN devices d ON r.id = d.room_id
                           LEFT JOIN device_data dd ON d.id = dd.device_id
                           LEFT JOIN sensors s ON r.id = s.room_id
                           LEFT JOIN sensor_data sd ON s.id = sd.sensor_id
                           WHERE r.id = @roomId
                           GROUP BY r.id, r.name, sd.parameter
                           """;
        var roomData = await connection.QueryAsync<RoomRawDto>(sql, new { roomId });

        var room = roomData
            .GroupBy(r => new { r.Id, r.Name, r.DeviceSpeed })
            .Select(g => new RoomDto
            {
                Id = g.Key.Id,
                Name = g.Key.Name,
                DeviceSpeed = g.Key.DeviceSpeed,
                Params = g
                    .Where(x => x.ParamKey != null)
                    .ToDictionary(x => x.ParamKey, x => x.ParamValue)
            }).FirstOrDefault();

        return room;
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
}