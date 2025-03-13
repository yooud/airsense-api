using Airsense.API.Models.Dto.Auth;
using Airsense.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Airsense.API.Controllers;

[ApiController]
[Route("mqtt")]
public class MqttController(IAuthMqttService authService) : ControllerBase
{
    [HttpPost("auth")]
    public async Task<IActionResult> MqttLogin([FromBody] MqttAuthRequestDto request)
    {
        var result = await authService.AuthenticateAsync(request);
        return Ok(new { result });
    }

    [HttpPost("acl")]
    public async Task<IActionResult> MqttAcl([FromBody] MqttAclRequestDto request)
    {
        var result = await authService.AuthorizeAsync(request);
        return Ok(new { result });
    }
}