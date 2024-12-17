using System.Data;
using Airsense.API.Models.Entity;
using Dapper;

namespace Airsense.API.Repository;

public class UserRepository(IDbConnection connection) : IUserRepository
{
    public async Task<bool> IsExistsByUidAsync(string uid)
    {
        const string sql = "SELECT 1 FROM users WHERE uid = @uid";
        var result = await connection.QueryAsync(sql, new { uid });
        return result.SingleOrDefault() != null;
    }

    public async Task<User> CreateAsync(User user)
    {
        const string sql = """
                           INSERT INTO users (uid, name, email) VALUES (@Uid, @Name, @Email)
                           RETURNING 
                               id AS Id, 
                               uid AS Uid, 
                               name AS Name, 
                               email AS Email, 
                               notification_token AS NotificationToken,
                               created_at AS CreatedAt;
                           """;
        var result = await connection.QuerySingleAsync<User>(sql, user);
        return result;
    }

    public async Task SetNotificationTokenAsync(string uid, string token)
    {
        const string sql = "UPDATE users SET notification_token = @token WHERE uid = @uid";
        await connection.ExecuteAsync(sql, new { uid, token });
    }
}