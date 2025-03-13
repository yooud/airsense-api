using Airsense.API.Models.Dto.Auth;

namespace Airsense.API.Services;

public interface IAuthMqttService
{
    public Task<string> AuthenticateAsync(MqttAuthRequestDto request);

    public Task<string> AuthorizeAsync(MqttAclRequestDto request);
}