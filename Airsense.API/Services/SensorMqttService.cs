using System.Text;
using Airsense.API.Models.Dto.Sensor;
using Airsense.API.Repository;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using MQTTnet;

namespace Airsense.API.Services;

public class SensorMqttService : MqttServiceBase
{
    public SensorMqttService(IServiceProvider serviceProvider, IOptions<JsonOptions> jsonOptions, MqttClientOptions mqttOptions)
        : base(serviceProvider, jsonOptions, mqttOptions)
    {
        RegisterCallback("sensor/+", OnSensorDataReceivedAsync);
    }

    private async Task OnSensorDataReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic.Split("/").ToList();
        var payload = Deserialize<SensorDataDto>(Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
        if (payload is null)
            return;
        payload.Parameter = topic[1];

        var serialNumber = e.ApplicationMessage.UserProperties.FirstOrDefault(p => p.Name == "serial-number")?.Value;
        if (serialNumber is null)
            return;

        using var scope = GetServiceProvider().CreateScope();
        var sensorRepository = scope.ServiceProvider.GetRequiredService<ISensorRepository>();
        var sensorDataProcessingService = scope.ServiceProvider.GetRequiredService<ISensorDataProcessingService>();

        var sensor = await sensorRepository.GetBySerialNumberAsync(serialNumber);
        if (sensor?.RoomId is null)
            return;

        var types = await sensorRepository.GetTypesAsync(sensor.Id);
        if (!types.Contains(payload.Parameter))
            return;

        Console.WriteLine(payload.Parameter);
        await sensorRepository.AddDataAsync(sensor.Id, payload);
        await Task.Run(() => sensorDataProcessingService.ProcessDataAsync(sensor.RoomId.Value, payload));
    }
}