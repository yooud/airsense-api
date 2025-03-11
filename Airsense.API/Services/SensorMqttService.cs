using System.Text;
using System.Text.Json;
using Airsense.API.Models.Dto.Sensor;
using Airsense.API.Repository;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace Airsense.API.Services;

public class MqttService : BackgroundService, IMqttService
{
    private readonly MqttClientFactory _mqttFactory;
    private readonly MqttClientOptions _mqttOptions;
    private readonly IServiceProvider _serviceProvider;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private IMqttClient? _mqttClient;

    public MqttService(IServiceProvider serviceProvider, IOptions<JsonOptions> jsonOptions, MqttClientOptions mqttOptions)
    {
        _serviceProvider = serviceProvider;
        _jsonSerializerOptions = jsonOptions.Value.SerializerOptions;
        _mqttFactory = new ();
        _mqttOptions = mqttOptions;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _mqttClient = _mqttFactory.CreateMqttClient();

        _mqttClient.ConnectedAsync += OnConnected;
        _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceived;

        await _mqttClient.ConnectAsync(_mqttOptions, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
            await Task.Delay(1000, stoppingToken);
    }

    private async Task OnConnected(MqttClientConnectedEventArgs e)
    {
        if (_mqttClient is not null)
        {
            var topicFilters = new List<MqttTopicFilter>
            {
                new MqttTopicFilterBuilder()
                    .WithTopic("sensor/+")
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build(),
            };

            await _mqttClient.SubscribeAsync(new MqttClientSubscribeOptions
            {
                TopicFilters = topicFilters
            });
        }
    }

    private async Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic.Split("/").ToList();
        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

        using var scope = _serviceProvider.CreateScope();

        if (topic[0] == "sensor")
        {
            var serialNumber = e.ApplicationMessage.UserProperties.FirstOrDefault(p => p.Name == "serial-number")?.Value;
            if (serialNumber is null)
                return;

            var data = JsonSerializer.Deserialize<SensorDataDto>(payload, _jsonSerializerOptions);
            if (data is null)
                return;
            data.Parameter = topic[1];

            var sensorRepository = scope.ServiceProvider.GetRequiredService<ISensorRepository>();
            var sensorDataProcessingService = scope.ServiceProvider.GetRequiredService<ISensorDataProcessingService>();

            var sensor = await sensorRepository.GetBySerialNumberAsync(serialNumber);

            if (sensor?.RoomId is null)
                return;

            var types = await sensorRepository.GetTypesAsync(sensor.Id);
            if (!types.Contains(data.Parameter))
                return;

            await sensorRepository.AddDataAsync(sensor.Id, data);
            await Task.Run(() => sensorDataProcessingService.ProcessDataAsync(sensor.RoomId.Value, data));
        }

        await Task.CompletedTask;
    }

    public Task PublishAsync(string topic, object payload) => PublishAsync(topic, JsonSerializer.Serialize(payload));

    public async Task PublishAsync(string topic, string payload)
    {
        if (_mqttClient is not null && _mqttClient.IsConnected)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();
            await _mqttClient.PublishAsync(message);
        }
    }
}