using System.Security.Claims;
using Airsense.API.Models.Dto.Room;
using Airsense.API.Models.Dto.Settings;
using Airsense.API.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Airsense.API.Controllers;

[ApiController]
[Route("room/{roomId:int}")]
[Authorize]
public class RoomController(
    IRoomRepository roomRepository,
    ISensorRepository sensorRepository,
    IDeviceRepository deviceRepository,
    ISettingsRepository settingsRepository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAvailableTypes(int roomId)
    {
        if (!int.TryParse(User.FindFirstValue("id"), out var userId))
            return BadRequest(new { message = "You are not registered" });

        var room = await roomRepository.GetByIdAsync(roomId);
        if (room is null)
            return NotFound(new { message = "Room not found" });
        
        if (!await roomRepository.IsHasAccessAsync(userId, roomId))
            return Forbid();
        
        var types = await roomRepository.GetAvailableTypesAsync(roomId);
        return Ok(types);
    }
    
    [HttpGet("{parameter}/curve")]
    public async Task<IActionResult> GetSensorSettings(int roomId, string parameter)
    {
        if (!int.TryParse(User.FindFirstValue("id"), out var userId))
            return BadRequest(new { message = "You are not registered" });

        var room = await roomRepository.GetByIdAsync(roomId);
        if (room is null)
            return NotFound(new { message = "Room not found" });
        
        if (!await roomRepository.IsHasAccessAsync(userId, roomId))
            return Forbid();

        var types = await roomRepository.GetAvailableTypesAsync(roomId);
        if (!types.Any(p => p.Name.Equals(parameter)))
            return BadRequest(new { message = "Parameter not found" });
        
        var curve = await settingsRepository.GetCurveAsync(roomId, parameter);
        if (curve is null)
        {
            curve = new CurveDto
            {
                CriticalValue = null,
                Points = new List<CurvePointDto>
                {
                    new() { Value = 0, FanSpeed = 0 },
                    new() { Value = 30, FanSpeed = 100 }
                }
            };
            await settingsRepository.UpdateCurveAsync(roomId, parameter, curve);
        }

        return Ok(curve);
    }
 
    [HttpPatch("{parameter}/curve")]
    public async Task<IActionResult> UpdateRoomSettings(int roomId, string parameter, [FromBody] UpdateCurveRequestDto request)
    {
        if (!int.TryParse(User.FindFirstValue("id"), out var userId))
            return BadRequest(new { message = "You are not registered" });

        var room = await roomRepository.GetByIdAsync(roomId);
        if (room is null)
            return NotFound(new { message = "Room not found" });
        
        if (!await roomRepository.IsHasAccessAsync(userId, roomId))
            return Forbid();
        
        var types = await roomRepository.GetAvailableTypesAsync(roomId);
        if (!types.Any(p => p.Name.Equals(parameter)))
            return BadRequest(new { message = "Parameter not found" });
        
        await settingsRepository.UpdateCurveAsync(roomId, parameter, request);
        return NoContent();
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetRoomDevicesHistory(
        int roomId, 
        [FromQuery] long? from, 
        [FromQuery] long? to,
        [FromQuery] HistoryDto.HistoryInterval interval = HistoryDto.HistoryInterval.Hour
    )
    {
        if (!int.TryParse(User.FindFirstValue("id"), out var userId))
            return BadRequest(new { message = "You are not registered" });

        var room = await roomRepository.GetByIdAsync(roomId);
        if (room is null)
            return NotFound(new { message = "Room not found" });
        
        if (!await roomRepository.IsHasAccessAsync(userId, roomId))
            return Forbid();

        var fromDate = from is null ? DateTime.UtcNow.AddDays(-3) : DateTime.UnixEpoch.AddMilliseconds(from.Value);
        var toDate = to is null ? DateTime.UtcNow : DateTime.UnixEpoch.AddMilliseconds(to.Value);
        
        var history = await deviceRepository.GetRoomHistoryAsync(roomId, fromDate, toDate, interval);
        return Ok(new HistoryDto
        {
            Data = history,
            Metadata = new HistoryDto.HistoryMetadata
            {
                From = new DateTimeOffset(fromDate).ToUnixTimeMilliseconds(),
                To = new DateTimeOffset(toDate).ToUnixTimeMilliseconds(),
                Interval = interval
            }
        });
    }
    
    [HttpGet("history/{deviceId:int}")]
    public async Task<IActionResult> GetDeviceHistory(
        int roomId, 
        int deviceId,
        [FromQuery] long? from, 
        [FromQuery] long? to,
        [FromQuery] HistoryDto.HistoryInterval interval = HistoryDto.HistoryInterval.Hour
    )
    {
        if (!int.TryParse(User.FindFirstValue("id"), out var userId))
            return BadRequest(new { message = "You are not registered" });

        var room = await roomRepository.GetByIdAsync(roomId);
        if (room is null)
            return NotFound(new { message = "Room not found" });
        
        if (!await roomRepository.IsHasAccessAsync(userId, roomId))
            return Forbid();
        
        var device = await deviceRepository.GetByIdAsync(deviceId);
        if (device is null)
            return NotFound(new { message = "Device not found" });
        
        if (device.RoomId != roomId)
            return BadRequest(new { message = "Device not found in this room" });

        var fromDate = from is null ? DateTime.UtcNow.AddDays(-3) : DateTime.UnixEpoch.AddMilliseconds(from.Value);
        var toDate = to is null ? DateTime.UtcNow : DateTime.UnixEpoch.AddMilliseconds(to.Value);
        
        var history = await deviceRepository.GetDeviceHistoryAsync(deviceId, fromDate, toDate, interval);
        return Ok(new HistoryDto
        {
            Data = history,
            Metadata = new HistoryDto.HistoryMetadata
            {
                From = new DateTimeOffset(fromDate).ToUnixTimeMilliseconds(),
                To = new DateTimeOffset(toDate).ToUnixTimeMilliseconds(),
                Interval = interval
            }
        });
    }
    
    [HttpGet("{parameter}/history")]
    public async Task<IActionResult> GetRoomSensorsHistory(
        int roomId, 
        string parameter, 
        [FromQuery] long? from, 
        [FromQuery] long? to,
        [FromQuery] HistoryDto.HistoryInterval interval = HistoryDto.HistoryInterval.Hour
    )
    {
        if (!int.TryParse(User.FindFirstValue("id"), out var userId))
            return BadRequest(new { message = "You are not registered" });

        var room = await roomRepository.GetByIdAsync(roomId);
        if (room is null)
            return NotFound(new { message = "Room not found" });
        
        if (!await roomRepository.IsHasAccessAsync(userId, roomId))
            return Forbid();
        
        var types = await roomRepository.GetAvailableTypesAsync(roomId);
        if (!types.Any(p => p.Name.Equals(parameter)))
            return BadRequest(new { message = "Parameter not found" });

        var fromDate = from is null ? DateTime.UtcNow.AddDays(-3) : DateTime.UnixEpoch.AddMilliseconds(from.Value);
        var toDate = to is null ? DateTime.UtcNow : DateTime.UnixEpoch.AddMilliseconds(to.Value);
        
        var history = await sensorRepository.GetRoomHistoryAsync(roomId, parameter, fromDate, toDate, interval);
        return Ok(new HistoryDto
        {
            Data = history,
            Metadata = new HistoryDto.HistoryMetadata
            {
                From = new DateTimeOffset(fromDate).ToUnixTimeMilliseconds(),
                To = new DateTimeOffset(toDate).ToUnixTimeMilliseconds(),
                Interval = HistoryDto.HistoryInterval.Hour
            }
        });
    }
    
    [HttpGet("{parameter}/history/{sensorId:int}")]
    public async Task<IActionResult> GetSensorHistory(
        int roomId, 
        string parameter, 
        int sensorId,
        [FromQuery] long? from, 
        [FromQuery] long? to,
        [FromQuery] HistoryDto.HistoryInterval interval = HistoryDto.HistoryInterval.Hour
    )
    {
        if (!int.TryParse(User.FindFirstValue("id"), out var userId))
            return BadRequest(new { message = "You are not registered" });

        var room = await roomRepository.GetByIdAsync(roomId);
        if (room is null)
            return NotFound(new { message = "Room not found" });
        
        if (!await roomRepository.IsHasAccessAsync(userId, roomId))
            return Forbid();
        
        var types = await roomRepository.GetAvailableTypesAsync(roomId);
        if (!types.Any(p => p.Name.Equals(parameter)))
            return BadRequest(new { message = "Parameter not found" });
        
        var sensor = await sensorRepository.GetByIdAsync(sensorId);
        if (sensor is null)
            return NotFound(new { message = "Sensor not found" });

        var sensorTypes = await sensorRepository.GetTypesAsync(sensor.Id);
        if (!sensorTypes.Any(p => p.Equals(parameter)))
            return BadRequest(new { message = "Parameter not found" });
        
        if (sensor.RoomId != roomId)
            return BadRequest(new { message = "Sensor not found in this room" });

        var fromDate = from is null ? DateTime.UtcNow.AddDays(-3) : DateTime.UnixEpoch.AddMilliseconds(from.Value);
        var toDate = to is null ? DateTime.UtcNow : DateTime.UnixEpoch.AddMilliseconds(to.Value);
        
        var history = await sensorRepository.GetSensorHistoryAsync(sensorId, parameter, fromDate, toDate, interval);
        return Ok(new HistoryDto
        {
            Data = history,
            Metadata = new HistoryDto.HistoryMetadata
            {
                From = new DateTimeOffset(fromDate).ToUnixTimeMilliseconds(),
                To = new DateTimeOffset(toDate).ToUnixTimeMilliseconds(),
                Interval = HistoryDto.HistoryInterval.Hour
            }
        });
    }
}