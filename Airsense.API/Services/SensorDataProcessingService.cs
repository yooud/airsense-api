using Airsense.API.Models.Dto.Sensor;
using Airsense.API.Models.Dto.Settings;
using Airsense.API.Repository;

namespace Airsense.API.Services;

public class SensorDataProcessingService(
    IDeviceRepository deviceRepository,
    IEnvironmentRepository environmentRepository,
    ISettingsRepository settingsRepository,
    IMqttService mqttService,
    INotificationService notificationService) : ISensorDataProcessingService
{
    public async Task ProcessDataAsync(int roomId, SensorDataDto data)
    {
        var curve = await settingsRepository.GetCurveAsync(roomId, data.Parameter);

        if (curve?.Points == null || curve.Points.Count == 0)
            return;

        var fanSpeed = GetFanSpeedByValue(curve.Points, data.Value);

        if (!fanSpeed.HasValue)
            return;
        
        await deviceRepository.AddDataAsync(roomId, fanSpeed.Value);
        await mqttService.PublishAsync($"room/{roomId}", new
        {
            fanSpeed,
            timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds(),
        });

        if (data.Value >= curve.CriticalValue)
        {
            var environment = await environmentRepository.GetByRoomIdAsync(roomId);
            if (environment is null)
                return;
            
            var membersTokens = await environmentRepository.GetMembersNotificationTokensAsync(environment.Id);
            if (membersTokens.Count == 0)
                return;
            
            await Task.Run(() => 
                notificationService.SendNotificationAsync(
                    membersTokens, 
                    "Critical value exceeded", 
                    $"Critical value exceeded for {data.Parameter} in  {environment.Name}"
                )
            );
        }
    }

    private static int? GetFanSpeedByValue(ICollection<CurvePointDto> points, double value)
    {
        var sortedPoints = points.OrderBy(p => p.Value).ToList();

        if (value <= sortedPoints[0].Value)
            return sortedPoints[0].FanSpeed;

        if (value >= sortedPoints.Last().Value)
            return sortedPoints.Last().FanSpeed;

        for (var i = 0; i < sortedPoints.Count - 1; i++)
        {
            var current = sortedPoints[i];
            var next = sortedPoints[i + 1];
            if (value >= current.Value && value <= next.Value)
            {
                var interpolatedFanSpeed = current.FanSpeed + (value - current.Value) * (next.FanSpeed - current.FanSpeed) / (next.Value - current.Value);
                return (int)Math.Round(interpolatedFanSpeed);
            }
        }

        return null;
    }
}