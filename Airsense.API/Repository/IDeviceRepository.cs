using Airsense.API.Models.Dto.Device;
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
}