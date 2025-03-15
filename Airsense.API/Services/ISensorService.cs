using Airsense.API.Models.Dto.Sensor;

namespace Airsense.API.Services;

public interface ISensorService
{
    public Task ProcessDataAsync(int roomId, string parameter, SensorDataDto data);
}