using Airsense.API.Models.Dto.Room;
using Airsense.API.Models.Entity;

namespace Airsense.API.Repository;

public interface IRoomRepository
{
    public Task<ICollection<RoomDto>> GetAsync(int envId, int count, int skip); 
    
    public Task<int> CountAsync(int envId);
    
    public Task<Room> CreateAsync(Room room);
    
    public Task<RoomDto?> GetByIdAsync(int roomId);
    
    public Task UpdateAsync(int roomId, string name);
    
    public Task<bool> IsExistsAsync(int roomId, int envId);
    
    public Task DeleteAsync(int roomId);
}