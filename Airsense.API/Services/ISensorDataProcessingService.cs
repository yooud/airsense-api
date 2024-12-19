using Airsense.API.Models.Dto.Sensor;

namespace Airsense.API.Services;

public interface ISensorDataProcessingService
{
    public Task ProcessDataAsync(int roomId, SensorDataDto data);
}