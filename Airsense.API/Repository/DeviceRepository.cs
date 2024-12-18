using System.Data;
using Airsense.API.Models.Dto.Device;
using Airsense.API.Models.Entity;
using Dapper;

namespace Airsense.API.Repository;

public class DeviceRepository(IDbConnection connection) : IDeviceRepository
{
    public async Task<ICollection<DeviceDto>> GetAsync(int roomId, int count, int skip)
    {
        const string sql = """
                           SELECT 
                               d.id AS Id,
                               d.serial_number AS SerialNumber,
                               d.active_at AS ActiveAt,
                               dd.value AS FanSpeed
                           FROM devices d
                           JOIN (
                               SELECT DISTINCT (device_id)
                                   device_id,value
                               FROM device_data
                               ORDER BY timestamp DESC
                           ) dd ON d.id = dd.device_id
                           WHERE d.room_id = @roomId
                           LIMIT @count 
                           OFFSET @skip
                           """;
        var devices = await connection.QueryAsync<DeviceDto>(sql, new { roomId, count, skip });
        return devices.ToList();
    }

    public async Task<Device?> GetByIdAsync(int deviceId)
    {
        const string sql = """
                           SELECT 
                               d.id AS Id,
                               d.serial_number AS SerialNumber,
                               d.room_id AS RoomId,
                               d.active_at AS ActiveAt
                           FROM devices d
                           WHERE d.id = @deviceId
                           """;
        var device = await connection.QueryFirstOrDefaultAsync<Device>(sql, new { deviceId });
        return device;
    }

    public async Task<Device?> GetBySerialNumberAsync(string serialNumber)
    {
        const string sql = """
                           SELECT 
                               d.id AS Id,
                               d.serial_number AS SerialNumber,
                               d.room_id AS RoomId,
                               d.active_at AS ActiveAt
                           FROM devices d
                           WHERE d.serial_number = @serialNumber
                           """;
        var device = await connection.QueryFirstOrDefaultAsync<Device>(sql, new { serialNumber });
        return device;
    }

    public async Task<int> CountAsync(int roomId)
    {
        const string sql = "SELECT COUNT(*) FROM devices d WHERE d.room_id = @roomId";
        var count = await connection.QueryFirstOrDefaultAsync<int>(sql, new { roomId });
        return count;
    }

    public async Task UpdateRoomAsync(int roomId, int deviceId)
    {
        const string sql = "UPDATE devices SET room_id = @roomId WHERE id = @deviceId";
        await connection.ExecuteAsync(sql, new { roomId, deviceId });
    }

    public async Task DeleteRoomAsync(int deviceId)
    {
        const string sql = "UPDATE devices SET room_id = NULL WHERE id = @deviceId";
        await connection.ExecuteAsync(sql, new { deviceId });
    }
}