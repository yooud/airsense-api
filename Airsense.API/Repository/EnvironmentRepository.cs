using System.Data;
using Airsense.API.Models.Dto.Environment;
using Dapper;
using Environment = Airsense.API.Models.Entity.Environment;

namespace Airsense.API.Repository;

public class EnvironmentRepository(IDbConnection connection) : IEnvironmentRepository
{
    public async Task<ICollection<EnvironmentDto>> GetAvailableAsync(int userId, int count, int skip)
    {
        const string sql = """
                           SELECT 
                               e.id AS Id,
                               e.name AS Name,
                               m.role AS Role
                           FROM environment_members m
                           JOIN environments e ON m.environment_id = e.id
                           WHERE m.member_id = @userId
                           LIMIT @count 
                           OFFSET @skip
                           """;
        var result = await connection.QueryAsync<EnvironmentDto>(sql, new { userId, count, skip });
        return result.ToList();
    }

    public async Task<int> CountAvailableAsync(int userId)
    {
        const string sql = "SELECT COUNT(*) FROM environment_members WHERE member_id = @userId";
        var result = await connection.QuerySingleAsync<int>(sql, new { userId });
        return result;
    }

    public async Task<Environment> CreateAsync(Environment environment, int userId)
    {
        const string insertEnvSql = "INSERT INTO environments (name) VALUES (@Name) RETURNING id";
        const string insertMemberSql = """
                                       INSERT INTO environment_members (environment_id, member_id, role) VALUES (@envId, @userId, 'owner');
                                       SELECT 
                                           e.id AS Id,
                                           e.name AS Name
                                       FROM environments e
                                       WHERE id = @envId
                                       """;
        
        var envId = await connection.QuerySingleAsync<int>(insertEnvSql, new { environment.Name });
        var result = await connection.QuerySingleAsync<Environment>(insertMemberSql, new { envId, userId });
        return result;
    }

    public async Task<Environment?> GetByIdAsync(int envId)
    {
        const string sql = """
                           SELECT 
                               e.id AS Id,
                               e.name AS Name,
                               m.role AS Role
                           FROM environment_members m
                           JOIN environments e ON m.environment_id = e.id
                           WHERE m.environment_id = @envId
                           """;
        
        var result = await connection.QueryFirstOrDefaultAsync<Environment>(sql, new { envId });
        return result;
    }

    public async Task<bool> IsMemberAsync(int userId, int environmentId)
    {
        const string sql = "SELECT 1 FROM environment_members WHERE member_id = @userId AND environment_id = @environmentId";
        var result = await connection.QueryAsync(sql, new { userId, environmentId });
        return result.SingleOrDefault() != null;
    }

    public async Task<bool> IsExistsAsync(int envId)
    {
        const string sql = "SELECT 1 FROM environments WHERE id = @envId";
        var result = await connection.QueryAsync(sql, new { envId });
        return result.SingleOrDefault() != null;
    }

    public async Task<ICollection<EnvironmentMemberDto>> GetMembersAsync(int envId, int count, int skip)
    {
        const string sql = """
                           SELECT 
                               u.id AS Id,
                               u.name AS Name,
                               u.email AS Email,
                               m.role AS Role
                           FROM environment_members m
                           JOIN users u ON m.member_id = u.id
                           WHERE m.environment_id = @envId
                           LIMIT @count
                           OFFSET @skip
                           """;
        var result = await connection.QueryAsync<EnvironmentMemberDto>(sql, new { envId, count, skip });
        return result.ToList();
    }

    public async Task<int> CountMembersAsync(int envId)
    {
        const string sql = "SELECT COUNT(*) FROM environment_members WHERE environment_id = @envId";
        var result = await connection.QuerySingleAsync<int>(sql, new { envId });
        return result;
    }

    public async Task<string?> GetRoleAsync(int userId, int envId)
    {
        const string sql = "SELECT role FROM environment_members WHERE member_id = @userId AND environment_id = @envId";
        var result = await connection.QuerySingleOrDefaultAsync<string>(sql, new { userId, envId });
        return result;
    }

    public async Task DeleteAsync(int envId)
    {
        const string sql = "DELETE FROM environments WHERE id = @envId";
        await connection.ExecuteAsync(sql, new { envId });
    }
    
    public async Task UpdateAsync(int envId, string name)
    {
        const string sql = "UPDATE environments SET name = @name WHERE id = @envId";
        await connection.ExecuteAsync(sql, new { envId, name });
    }
    
    public async Task AddMemberAsync(int envId, int userId)
    {
        const string sql = "INSERT INTO environment_members (environment_id, member_id, role) VALUES (@envId, @userId, 'user')";
        await connection.ExecuteAsync(sql, new { envId, userId });
    }
    
    public async Task RemoveMemberAsync(int envId, int userId)
    {
        const string sql = "DELETE FROM environment_members WHERE environment_id = @envId AND member_id = @userId";
        await connection.ExecuteAsync(sql, new { envId, userId });
    }
    
    public async Task UpdateMemberAsync(int envId, int userId, string role)
    {
        const string sql = "UPDATE environment_members SET role = @role WHERE environment_id = @envId AND member_id = @userId";
        await connection.ExecuteAsync(sql, new { envId, userId, role });
    }
    
    public async Task<ICollection<string>> GetMembersNotificationTokensAsync(int envId)
    {
        const string sql = """
                           SELECT u.notification_token
                           FROM environment_members m
                           JOIN users u ON m.member_id = u.id
                           WHERE m.environment_id = @envId
                           """;
        var result = await connection.QueryAsync<string>(sql, new { envId });
        return result.ToList();
    }
    
    public async Task<Environment?> GetByRoomIdAsync(int roomId)
    {
        const string sql = """
                           SELECT 
                               e.id AS Id,
                               e.name AS Name
                           FROM rooms r
                           JOIN environments e ON e.id = r.environment_id
                           WHERE r.id = @roomId
                           """;
        var result = await connection.QueryFirstOrDefaultAsync<Environment>(sql, new { roomId });
        return result;
    }
}