using System.Security.Cryptography;
using System.Text;
using Airsense.API.Models.Dto.Auth;
using Airsense.API.Repository;

namespace Airsense.API.Services;

public class AuthMqttService(IDeviceRepository deviceRepository, ISensorRepository sensorRepository) : IAuthMqttService
{
    private static string _apiSecret = string.Empty;

    public async Task<string> AuthenticateAsync(MqttAuthRequestDto request)
    {
        var targetSecret = string.Empty;
        if (request.ClientId.StartsWith("s-"))
            targetSecret = await GetSensorSecretAsync(request.Username);
        else if (request.ClientId.StartsWith("d-"))
            targetSecret = await GetDeviceSecretAsync(request.Username);
        else if (request.ClientId.Equals("api"))
            targetSecret = _apiSecret;

        if (string.IsNullOrEmpty(targetSecret))
            return "ignore";

        if (targetSecret.Equals(ComputeMd5Hash(request.Password + request.Username)))
            return "allow";

        return "deny";
    }

    public async Task<string> AuthorizeAsync(MqttAclRequestDto request)
    {
        var result = "ignore";
        if (request.ClientId.StartsWith("s-"))
            result = await AuthorizeSensorAsync(request);
        else if (request.ClientId.StartsWith("d-"))
            result = await AuthorizeDeviceAsync(request);
        else if (request.ClientId.Equals("api"))
            result = await AuthorizeApiServerAsync(request);

        return result;
    }

    internal static string GetApiCredentials()
    {
        var password = Guid.NewGuid().ToString();
        _apiSecret = ComputeMd5Hash(password + "api");
        return password;
    }

    private async Task<string> GetDeviceSecretAsync(string serialNumber)
    {
        var device = await deviceRepository.GetBySerialNumberAsync(serialNumber);
        if (device is null)
            return string.Empty;

        return device.Secret;
    }

    private async Task<string> GetSensorSecretAsync(string serialNumber)
    {
        var sensor = await sensorRepository.GetBySerialNumberAsync(serialNumber);
        if (sensor is null)
            return string.Empty;

        return sensor.Secret;
    }

    private async Task<string> AuthorizeDeviceAsync(MqttAclRequestDto request)
    {
        var device = await deviceRepository.GetBySerialNumberAsync(request.Username);
        if (device is null)
            return "ignore";

        if (request.Action.Equals("publish"))
            return "deny";

        var topic = request.Topic.Split("/");
        if (topic[0].Equals("room"))
        {
            if (topic[1].Equals(device.RoomId?.ToString()))
                return "allow";
        }
        else if (topic[0].Equals("device"))
        {
            if (topic[1].Equals(device.Id.ToString()))
                return "allow";
        }

        return "deny";
    }

    private async Task<string> AuthorizeSensorAsync(MqttAclRequestDto request)
    {
        var sensor = await sensorRepository.GetBySerialNumberAsync(request.Username);
        if (sensor is null)
            return "ignore";

        if (request.Action.Equals("subscribe"))
            return "deny";

        var topic = request.Topic.Split("/");
        if (topic[0].Equals("sensor"))
        {
            var types = await sensorRepository.GetTypesAsync(sensor.Id);
            if (types.Any(t => topic[1].Equals(t)))
                return "allow";
        }

        return "deny";
    }

    private Task<string> AuthorizeApiServerAsync(MqttAclRequestDto request) => Task.FromResult("allow");

    private static string ComputeMd5Hash(string input)
    {
        using MD5 md5 = MD5.Create();
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = md5.ComputeHash(inputBytes);

        StringBuilder sb = new StringBuilder();
        foreach (var b in hashBytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}