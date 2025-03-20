using Airsense.API.Models.Dto.Room;
using Airsense.API.Models.Dto.Sensor;
using Airsense.API.Models.Entity;

namespace Airsense.API.Repository;

public interface IRoomRepository
{
    public Task<ICollection<RoomDto>> GetAsync(int envId, int skip, int count); 
    
    public Task<int> CountAsync(int envId);
    
    public Task<Room> CreateAsync(Room room);
    
    public Task<RoomDto?> GetByIdAsync(int roomId);
    
    public Task UpdateAsync(int roomId, string name);
    
    public Task<bool> IsExistsAsync(int roomId, int envId);
    
    public Task DeleteAsync(int roomId);
    
    public Task<bool> IsHasAccessAsync(int userId, int roomId);
    
    public Task<ICollection<ParameterDto>> GetAvailableTypesAsync(int roomId);
}