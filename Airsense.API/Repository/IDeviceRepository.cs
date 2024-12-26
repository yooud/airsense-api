using Airsense.API.Models.Dto.Device;
using Airsense.API.Models.Dto.Room;
using Airsense.API.Models.Entity;

namespace Airsense.API.Repository;

public interface IDeviceRepository
{
    public Task<ICollection<DeviceDto>> GetAsync(int roomId, int count, int skip);
    
    public Task<Device?> GetByIdAsync(int deviceId);
    
    public Task<Device?> GetBySerialNumberAsync(string serialNumber);
    
    public Task<int> CountAsync(int roomId);
    
    public Task UpdateRoomAsync(int roomId, int deviceId);
    
    public Task DeleteRoomAsync(int deviceId);
    
    public Task AddDataAsync(int roomId, double speed);
    
    public Task<double?> GetFanSpeedAsync(string serialNumber);
    
    public Task<ICollection<HistoryDeviceDto>> GetRoomHistoryAsync(int roomId, DateTime fromDate, DateTime toDate, HistoryDto.HistoryInterval interval);
    
    public Task<HistoryDeviceDto?> GetDeviceHistoryAsync(int deviceId, DateTime fromDate, DateTime toDate, HistoryDto.HistoryInterval interval);
}