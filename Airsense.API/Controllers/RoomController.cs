using System.Security.Claims;
using Airsense.API.Models.Dto.Settings;
using Airsense.API.Repository;
using Microsoft.AspNetCore.Mvc;

namespace Airsense.API.Controllers;

[ApiController]
[Route("room/{roomId:int}")]
public class RoomController(
    IRoomRepository roomRepository,
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
    
    [HttpGet("{parameter}")]
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
        if (!types.Any(t => t.Equals(parameter)))
            return BadRequest(new { message = "Parameter not found" });
        
        var curve = await settingsRepository.GetCurveAsync(roomId, parameter);
        if (curve is null)
        {
            curve = new CurveDto
            {
                CriticalValue = 0,
                Points =
                {
                    new CurvePointDto { Value = 0, FanSpeed = 0 },
                    new CurvePointDto { Value = 30, FanSpeed = 100 }
                }
            };
            await settingsRepository.UpdateCurveAsync(roomId, parameter, curve);
        }

        return Ok(curve);
    }
 
    [HttpPatch("{parameter}")]
    public async Task<IActionResult> UpdateRoomSettings(int roomId, string parameter, [FromBody] UpdateCurveRequestDto request)
    {
        if (!int.TryParse(User.FindFirstValue("id"), out var userId))
            return BadRequest(new { message = "You are not registered" });

        var room = await roomRepository.GetByIdAsync(roomId);
        if (room is null)
            return NotFound(new { message = "Room not found" });
        
        if (!await roomRepository.IsHasAccessAsync(userId, roomId))
            return Forbid();
        
        await settingsRepository.UpdateCurveAsync(roomId, parameter, request);
        return NoContent();
    }
}