using Airsense.API.Repository;
using Microsoft.AspNetCore.Mvc;

namespace Airsense.API.Controllers;

[ApiController]
[Route("device")]
public class DeviceController(
    IDeviceRepository deviceRepository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUpdates([FromHeader(Name = "X-Serial-Number")] string serialNumber)
    {
        var speed = await deviceRepository.GetFanSpeedAsync(serialNumber);
        return Ok(new { fanSpeed = speed });
    }
}