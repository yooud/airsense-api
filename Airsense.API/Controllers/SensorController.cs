using Airsense.API.Models.Dto.Sensor;
using Airsense.API.Repository;
using Airsense.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Airsense.API.Controllers;

[ApiController]
[Route("sensor")]
public class SensorController(
    ISensorRepository sensorRepository,
    ISensorDataProcessingService sensorDataProcessingService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> SendData(
        [FromHeader(Name = "X-Serial-Number")] string serialNumber,
        [FromBody] SensorDataDto request
    )
    {
        var sensor = await sensorRepository.GetBySerialNumberAsync(serialNumber);
        if (sensor.RoomId is null)
            return BadRequest(new { message = "Sensor is not assigned to a room" });
        
        var types = await sensorRepository.GetTypesAsync(sensor.Id);
        if (!types.Contains(request.Parameter))
            return BadRequest(new { message = "Invalid parameter" });
        
        await sensorRepository.AddDataAsync(sensor.Id, request);
        await Task.Run(() => sensorDataProcessingService.ProcessDataAsync(sensor.RoomId.Value, request));
        return NoContent();
    }
}