using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Airsense.API.Models.Dto;
using Airsense.API.Models.Dto.Sensor;
using Airsense.API.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Airsense.API.Controllers;

[ApiController]
[Route("room/{roomId:int}/sensor")]
[Authorize]
public class RoomSensorController(
    IRoomRepository roomRepository,
    ISensorRepository sensorRepository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSensors(
        int roomId,
        [FromQuery][Range(0, int.MaxValue, ErrorMessage = "The skip parameter must be a non-negative integer.")] int skip = 0,
        [FromQuery][Range(0, int.MaxValue, ErrorMessage = "The count parameter must be a non-negative integer.")] int count = 10
    )
    {
        if (!int.TryParse(User.FindFirstValue("id"), out var userId))
            return BadRequest(new { message = "You are not registered" });

        var room = await roomRepository.GetByIdAsync(roomId);
        if (room is null)
            return NotFound(new { message = "Room not found" });
        
        if (!await roomRepository.IsHasAccessAsync(userId, roomId))
            return Forbid();
        
        var sensors = await sensorRepository.GetAsync(roomId, count, skip);
        var totalCount = await sensorRepository.CountAsync(roomId);
        
        return Ok(new PaginatedListDto
        {
            Data = sensors,
            Pagination = new PaginatedListDto.Metadata
            {
                Skip = skip,
                Count = sensors.Count,
                Total = totalCount
            }
        });
    }
    
    [HttpPost]
    public async Task<IActionResult> AddSensor(int roomId, [FromBody] AddRequestDto request)
    {
        if (!int.TryParse(User.FindFirstValue("id"), out var userId))
            return BadRequest(new { message = "You are not registered" });

        var room = await roomRepository.GetByIdAsync(roomId);
        if (room is null)
            return NotFound(new { message = "Room not found" });
        
        if (!await roomRepository.IsHasAccessAsync(userId, roomId))
            return Forbid();
        
        var sensor = await sensorRepository.GetBySerialNumberAsync(request.SerialNumber);
        if (sensor is null)
            return BadRequest(new { message = "Sensor not found" });
        
        if (sensor.RoomId != null)
            return BadRequest(new { message = "Sensor already in use" });
        
        await sensorRepository.UpdateRoomAsync(sensor.Id, roomId);
        return StatusCode(201, new SensorDto
        {
            Id = sensor.Id,
            SerialNumber = sensor.SerialNumber
        });
    }
    
    [HttpDelete("{sensorId:int}")]
    public async Task<IActionResult> RemoveSensor(int roomId, int sensorId)
    {
        if (!int.TryParse(User.FindFirstValue("id"), out var userId))
            return BadRequest(new { message = "You are not registered" });

        var room = await roomRepository.GetByIdAsync(roomId);
        if (room is null)
            return NotFound(new { message = "Room not found" });
        
        if (!await roomRepository.IsHasAccessAsync(userId, roomId))
            return Forbid();
        
        var sensor = await sensorRepository.GetByIdAsync(sensorId);
        if (sensor is null)
            return BadRequest(new { message = "Sensor not found" });
        
        if (sensor.RoomId != roomId)
            return BadRequest(new { message = "Sensor not in this room" });
        
        await sensorRepository.DeleteRoomAsync(sensorId);
        return NoContent();
    }
}