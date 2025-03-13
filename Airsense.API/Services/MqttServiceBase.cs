using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;

namespace Airsense.API.Services;

public abstract class MqttServiceBase(
    IServiceProvider serviceProvider,
    IOptions<JsonOptions> jsonOptions,
    MqttClientOptions mqttOptions) : BackgroundService, IMqttService
{
    private readonly MqttClientFactory _mqttFactory = new ();
    private IMqttClient? _mqttClient;
    private readonly ConcurrentDictionary<string, Func<MqttApplicationMessageReceivedEventArgs, Task>> _callbacks = new();

    public void RegisterCallback(string topicFilter, Func<MqttApplicationMessageReceivedEventArgs, Task> callback) => _callbacks[topicFilter] = callback;

    private async Task HandleReceivedMessageAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        string topic = e.ApplicationMessage.Topic;

        foreach (var kvp in _callbacks)
            if (IsTopicMatch(kvp.Key, topic))
                await kvp.Value(e);
    }

    private bool IsTopicMatch(string filter, string topic)
    {
        if (filter.Equals(topic, StringComparison.OrdinalIgnoreCase))
            return true;

        string regexPattern = "^" + Regex.Escape(filter)
            .Replace("\\+", "[^/]+")
            .Replace("\\#", ".*") + "$";

        return Regex.IsMatch(topic, regexPattern, RegexOptions.IgnoreCase);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _mqttClient = _mqttFactory.CreateMqttClient();

        _mqttClient.ConnectedAsync += OnConnected;
        _mqttClient.ApplicationMessageReceivedAsync += HandleReceivedMessageAsync;

        mqttOptions.Credentials =
            new MqttClientCredentials("api", Encoding.UTF8.GetBytes(AuthMqttService.GetApiCredentials()));

        await _mqttClient.ConnectAsync(mqttOptions, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
            await Task.Delay(1000, stoppingToken);
    }

    private async Task OnConnected(MqttClientConnectedEventArgs e)
    {
        if (_mqttClient is not null)
        {
            var topicFilters = _callbacks
                .Select(t => new MqttTopicFilterBuilder()
                    .WithTopic(t.Key)
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build()
                )
                .ToList();

            await _mqttClient.SubscribeAsync(new MqttClientSubscribeOptions
            {
                TopicFilters = topicFilters
            });
        }
    }

    protected T? Deserialize<T>(string data) => JsonSerializer.Deserialize<T>(data, jsonOptions.Value.SerializerOptions);

    protected IServiceProvider GetServiceProvider() => serviceProvider;

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

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_mqttClient is not null && _mqttClient.IsConnected)
        {
            var disconnectOptions = new MqttClientDisconnectOptionsBuilder()
                .WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection)
                .Build();
            await _mqttClient.DisconnectAsync(disconnectOptions, cancellationToken);
        }

        await base.StopAsync(cancellationToken);
    }
}