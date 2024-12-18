using System.Data;
using Airsense.API.Models.Dto.Settings;
using Dapper;
using Newtonsoft.Json;

namespace Airsense.API.Repository;

public class SettingsRepository(IDbConnection connection) : ISettingsRepository
{
    public async Task UpdateCurveAsync(int roomId, string parameter, object curve)
    {
        const string sql = """
                           INSERT INTO settings (room_id, parameter, curve)
                           VALUES (@roomId, @parameter, @curve::json)
                           ON CONFLICT (room_id, parameter) DO UPDATE
                           SET curve = @curve::json;
                           """;
        var curveJson = JsonConvert.SerializeObject(curve);
        await connection.ExecuteAsync(sql, new { roomId, parameter, curveJson });
    }
    
    public async Task<CurveDto?> GetCurveAsync(int roomId, string parameter)
    {
        const string sql = "SELECT curve FROM settings WHERE room_id = @roomId AND parameter = @parameter";
        var curveJson = await connection.QuerySingleOrDefaultAsync<string>(sql, new { roomId, parameter });
        if (curveJson is null)
            return null;
        
        var curve = JsonConvert.DeserializeObject<CurveDto>(curveJson);
        return curve;
    }
}