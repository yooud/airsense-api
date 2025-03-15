namespace Airsense.API.Models.Dto.Auth;

public class MqttAclRequestDto
{
    public string ClientId { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Topic { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;
}