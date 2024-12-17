using Airsense.API.Models.Dto.Environment;
using Environment = Airsense.API.Models.Entity.Environment;

namespace Airsense.API.Repository;

public interface IEnvironmentRepository
{
    public Task<ICollection<EnvironmentDto>> GetAvailableAsync(int userId, int count, int skip);
    
    public Task<int> CountAvailableAsync(int userId);
    
    public Task<Environment> CreateAsync(Environment environment, int userId);

    public Task<EnvironmentDto?> GetByIdAsync(int envId, int userId);
    
    public Task<bool> IsMemberAsync(int userId, int envId);
    
    public Task<bool> IsExistsAsync(int envId);
    
    public Task<ICollection<EnvironmentMemberDto>> GetMembersAsync(int envId, int count, int skip);
    
    public Task<int> CountMembersAsync(int envId);
    
    public Task<string?> GetRoleAsync(int userId, int envId);
    
    public Task DeleteAsync(int envId);
    
    public Task UpdateAsync(int envId, string name);
    
    public Task AddMemberAsync(int envId, int userId);
    
    public Task RemoveMemberAsync(int envId, int userId);
    
    public Task UpdateMemberAsync(int envId, int userId, string role);
}