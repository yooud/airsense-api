namespace Airsense.API.Services;

public interface IMqttService
{
    public Task PublishAsync(string topic, object payload);

    public Task PublishAsync(string topic, string payload);
}