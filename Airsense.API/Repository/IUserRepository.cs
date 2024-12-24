using Airsense.API.Models.Entity;

namespace Airsense.API.Repository;

public interface IUserRepository
{
    public Task<bool> IsExistsByUidAsync(string uid);
    
    public Task<User> CreateAsync(User user);
    
    public Task SetNotificationTokenAsync(string uid, string token);
    
    public Task<User?> GetByEmailAsync(string email);
}