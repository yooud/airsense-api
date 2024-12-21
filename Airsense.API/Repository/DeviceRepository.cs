using System.Data;
using Airsense.API.Models.Dto.Device;
using Airsense.API.Models.Dto.Room;
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
                               EXTRACT(EPOCH FROM dd.applied_at) AS ActiveAt,
                               dd.DeviceSpeed AS FanSpeed
                           FROM devices d
                           LEFT JOIN (
                               SELECT DISTINCT ON (dd.device_id)
                                   dd.device_id,
                                   dd.applied_at,
                                   dd.value AS DeviceSpeed
                               FROM device_data dd
                               WHERE dd.applied_at IS NOT NULL
                               ORDER BY dd.device_id, dd.applied_at DESC, dd.value DESC
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
                               d.room_id AS RoomId
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
                               d.room_id AS RoomId
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
    
    public async Task AddDataAsync(int roomId, double speed)
    {
        const string sql = "INSERT INTO device_data (device_id, value) SELECT d.id, @speed FROM devices d WHERE d.room_id = @roomId";
        await connection.ExecuteAsync(sql, new { roomId, speed });
    }
    
    public async Task<double?> GetFanSpeedAsync(string serialNumber)
    {
        const string selectSql = """
                               SELECT MAX(dd.value)
                               FROM devices d
                               JOIN device_data dd ON d.id = dd.device_id
                               WHERE d.serial_number = @serialNumber 
                                 AND dd.applied = false;
                               """;
        const string updateSql = """
                                 UPDATE device_data 
                                 SET applied = true, applied_at = @timestamp
                                 WHERE device_id = (SELECT d.id FROM devices d WHERE d.serial_number = @serialNumber) AND applied = false;
                                 """;
        var speed = await connection.QueryFirstOrDefaultAsync<double?>(selectSql, new { serialNumber });
        await connection.ExecuteAsync(updateSql, new { serialNumber, timestamp = DateTime.UtcNow });
        return speed;
    }
    
    public async Task<ICollection<HistoryDeviceDto>> GetRoomHistoryAsync(int roomId, DateTime fromDate, DateTime toDate, string interval)
    {
        switch (interval.ToLower())
        {
            case "minute":
                interval = "date_trunc('minute', dd.timestamp)";
                break;
            case "day":
                interval = "date_trunc('day', dd.timestamp)";
                break;
            case "hour":
            default:
                interval = "date_trunc('hour', dd.timestamp)";
                break;
        }

        var sql = $"""
                   SELECT 
                       d.id AS Id,
                       d.serial_number AS SerialNumber,
                       EXTRACT(EPOCH FROM {interval}) AS Timestamp,
                       AVG(dd.value) AS Value
                   FROM devices d
                   LEFT JOIN device_data dd ON dd.device_id = d.id
                   WHERE d.room_id = @roomId
                   AND dd.timestamp BETWEEN @fromDate AND @toDate
                   AND dd.applied = true
                   GROUP BY d.id, {interval}
                   ORDER BY d.id, {interval}
                   """;

        var historyData = await connection.QueryAsync<HistoryRawDto>(sql, new { roomId, fromDate, toDate });

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
    
    public async Task<object> GetDeviceHistoryAsync(int deviceId, DateTime fromDate, DateTime toDate, string interval)
    {
        switch (interval.ToLower())
        {
            case "minute":
                interval = "date_trunc('minute', dd.timestamp)";
                break;
            case "day":
                interval = "date_trunc('day', dd.timestamp)";
                break;
            case "hour":
            default:
                interval = "date_trunc('hour', dd.timestamp)";
                break;
        }

        var sql = $"""
                   SELECT 
                       d.id AS Id,
                       d.serial_number AS SerialNumber,
                       EXTRACT(EPOCH FROM {interval}) AS Timestamp,
                       AVG(dd.value) AS Value
                   FROM devices d
                   LEFT JOIN device_data dd ON dd.device_id = d.id
                   WHERE d.id = @deviceId
                   AND dd.timestamp BETWEEN @fromDate AND @toDate
                   AND dd.applied = true
                   GROUP BY d.id, {interval}
                   ORDER BY d.id, {interval}
                   """;

        var historyData = await connection.QueryAsync<HistoryRawDto>(sql, new { deviceId, fromDate, toDate });

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