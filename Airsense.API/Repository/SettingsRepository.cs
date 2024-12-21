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
                           INSERT INTO settings (room_id, parameter_id, curve)
                           SELECT @roomId, p.id, @curve::json
                           FROM parameters p
                           WHERE p.name = @parameter
                           ON CONFLICT (room_id, parameter_id) DO UPDATE
                           SET curve = @curve::json;
                           """;
        var curveJson = JsonConvert.SerializeObject(curve);
        await connection.ExecuteAsync(sql, new { roomId, parameter, curve = curveJson });
    }
    
    public async Task<CurveDto?> GetCurveAsync(int roomId, string parameter)
    {
        const string sql = """
                           SELECT s.curve 
                           FROM settings s
                           JOIN parameters p on p.id = s.parameter_id
                           WHERE room_id = @roomId 
                           AND p.name = @parameter
                           """;
        var curveJson = await connection.QuerySingleOrDefaultAsync<string>(sql, new { roomId, parameter });
        if (curveJson is null)
            return null;
        
        var curve = JsonConvert.DeserializeObject<CurveDto>(curveJson);
        return curve;
    }
}