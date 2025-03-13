namespace Airsense.API.Models.Dto.Auth;

public class MqttAuthRequestDto
{
    public string ClientId { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}