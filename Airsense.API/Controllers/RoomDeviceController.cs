using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Airsense.API.Models.Dto;
using Airsense.API.Models.Dto.Device;
using Airsense.API.Repository;
using Airsense.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Airsense.API.Controllers;

[ApiController]
[Route("room/{roomId:int}/device")]
[Authorize]
public class RoomDeviceController(
    IRoomRepository roomRepository,
    IDeviceRepository deviceRepository,
    IMqttService mqttService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetDevices(
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
        
        var devices = await deviceRepository.GetAsync(roomId, count, skip);
        var totalCount = await deviceRepository.CountAsync(roomId);
        
        return Ok(new PaginatedListDto
        {
            Data = devices,
            Pagination = new PaginatedListDto.Metadata
            {
                Skip = skip,
                Count = devices.Count,
                Total = totalCount
            }
        });
    }
    
    [HttpPost]
    public async Task<IActionResult> AddDevice(int roomId, [FromBody] AddRequestDto request)
    {
        if (!int.TryParse(User.FindFirstValue("id"), out var userId))
            return BadRequest(new { message = "You are not registered" });

        var room = await roomRepository.GetByIdAsync(roomId);
        if (room is null)
            return NotFound(new { message = "Room not found" });
        
        if (!await roomRepository.IsHasAccessAsync(userId, roomId))
            return Forbid();
        
        var device = await deviceRepository.GetBySerialNumberAsync(request.SerialNumber);
        if (device is null)
            return NotFound(new { message = "Device not found" });
        
        if (device.RoomId is not null)
            return BadRequest(new { message = "Device already in use" });
        
        await deviceRepository.UpdateRoomAsync(roomId, device.Id);
        return StatusCode(201, new DeviceDto
        {
            Id = device.Id,
            SerialNumber = device.SerialNumber
        });
    }
    
    [HttpDelete("{deviceId:int}")]
    public async Task<IActionResult> RemoveDevice(int roomId, int deviceId)
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
            return BadRequest(new { message = "Device not in this room" });
        
        await deviceRepository.DeleteRoomAsync(device.Id);
        await mqttService.PublishAsync($"device/{deviceId}", new
        {
            Action = "disconnect"
        });
        
        return NoContent();
    }
}