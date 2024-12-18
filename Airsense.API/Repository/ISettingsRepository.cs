using Airsense.API.Models.Dto.Settings;

namespace Airsense.API.Repository;

public interface ISettingsRepository
{
    public Task UpdateCurveAsync(int roomId, string parameter, object curve);
    
    public Task<CurveDto?> GetCurveAsync(int roomId, string parameter);
}