using Airsense.API.Repository;
using Airsense.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Airsense.API.Controllers;

[ApiController]
[Route("device")]
public class DeviceController(
    IAuthMqttService authService,
    IDeviceRepository deviceRepository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRoomId(
        [FromHeader(Name = "Authorization")] string authorization,
        [FromHeader(Name = "Client-Id")] string clientId
    )
    {
        var data = Convert.FromBase64String(authorization.Split("Basic ")[1]);
        var decodedString = System.Text.Encoding.UTF8.GetString(data);
        
        var parts = decodedString.Split(':');
        if (parts.Length != 2) 
            return Unauthorized();
        var username = parts[0];
        var password = parts[1];

        var result = await authService.AuthenticateAsync(new()
        {
            ClientId = clientId,
            Username = username,
            Password = password
        });

        if (!result.Equals("allow"))
            return Unauthorized();
        
        var device = await deviceRepository.GetBySerialNumberAsync(username);
        if (device is null)
            return BadRequest();
        
        return Ok(new { device.RoomId });
    }
}