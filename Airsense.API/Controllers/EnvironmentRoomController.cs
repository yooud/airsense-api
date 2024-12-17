using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Airsense.API.Models.Dto;
using Airsense.API.Models.Dto.Room;
using Airsense.API.Models.Entity;
using Airsense.API.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Airsense.API.Controllers;

[ApiController]
[Route("env/{envId:int}/room")]
[Authorize]
public class EnvironmentRoomController(
    IEnvironmentRepository environmentRepository,
    IRoomRepository roomRepository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRooms(
        int envId,
        [FromQuery][Range(0, int.MaxValue, ErrorMessage = "The skip parameter must be a non-negative integer.")] int skip = 0,
        [FromQuery][Range(0, int.MaxValue, ErrorMessage = "The count parameter must be a non-negative integer.")] int count = 10
    )
    {
        if (!int.TryParse(User.FindFirstValue("id"), out var userId))
            return BadRequest(new { message = "You are not registered" });

        var isExists = await environmentRepository.IsExistsAsync(envId);
        if (!isExists)
            return NotFound(new { message = "Environment not found" });
        
        if (!await environmentRepository.IsMemberAsync(userId, envId))
            return Forbid();
        
        var rooms = await roomRepository.GetAsync(envId, skip, count);
        var totalCount = await roomRepository.CountAsync(envId);
        
        return Ok(new PaginatedListDto
        {
            Data = rooms,
            Pagination = new PaginatedListDto.Metadata
            {
                Skip = skip,
                Count = rooms.Count,
                Total = totalCount
            }
        });
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateRoom(int envId, [FromBody] CreateRequestDto request)
    {
        if (!int.TryParse(User.FindFirstValue("id"), out var userId))
            return BadRequest(new { message = "You are not registered" });

        var isExists = await environmentRepository.IsExistsAsync(envId);
        if (!isExists)
            return NotFound(new { message = "Environment not found" });
        
        var role = await environmentRepository.GetRoleAsync(userId, envId);
        switch (role)
        {
            case null:
            case "user":
                return Forbid();
        }
        
        var room = new Room
        {
            EnvironmentId = envId,
            Name = request.Name
        };
        room = await roomRepository.CreateAsync(room);
        
        return CreatedAtAction(nameof(GetRooms), new { room.EnvironmentId, room.Id }, null);
    }
    
    [HttpGet("{roomId:int}")]
    public async Task<IActionResult> GetRoom(int envId, int roomId)
    {
        if (!int.TryParse(User.FindFirstValue("id"), out var userId))
            return BadRequest(new { message = "You are not registered" });

        var envIsExists = await environmentRepository.IsExistsAsync(envId);
        if (!envIsExists)
            return NotFound(new { message = "Environment not found" });
        
        if (!await environmentRepository.IsMemberAsync(userId, envId))
            return Forbid();
        
        var roomIsExists = await roomRepository.IsExistsAsync(roomId, envId);
        if (!roomIsExists)
            return NotFound(new { message = "Room not found" });
        
        var room = await roomRepository.GetByIdAsync(roomId);
        return Ok(room);
    }
    
    [HttpPatch("{roomId:int}")]
    public async Task<IActionResult> UpdateRoom(int envId, int roomId, [FromBody] UpdateRequestDto request)
    {
        if (!int.TryParse(User.FindFirstValue("id"), out var userId))
            return BadRequest(new { message = "You are not registered" });

        var envIsExists = await environmentRepository.IsExistsAsync(envId);
        if (!envIsExists)
            return NotFound(new { message = "Environment not found" });
        
        var role = await environmentRepository.GetRoleAsync(userId, envId);
        switch (role)
        {
            case null:
            case "user":
                return Forbid();
        }
        
        var roomIsExists = await roomRepository.IsExistsAsync(roomId, envId);
        if (!roomIsExists)
            return NotFound(new { message = "Room not found" });
        
        await roomRepository.UpdateAsync(roomId, request.Name);
        return NoContent();
    }
    
    [HttpDelete("{roomId:int}")]
    public async Task<IActionResult> DeleteRoom(int envId, int roomId)
    {
        if (!int.TryParse(User.FindFirstValue("id"), out var userId))
            return BadRequest(new { message = "You are not registered" });

        var envIsExists = await environmentRepository.IsExistsAsync(envId);
        if (!envIsExists)
            return NotFound(new { message = "Environment not found" });
        
        var role = await environmentRepository.GetRoleAsync(userId, envId);
        switch (role)
        {
            case null:
            case "user":
                return Forbid();
        }
        
        var roomIsExists = await roomRepository.IsExistsAsync(roomId, envId);
        if (!roomIsExists)
            return NotFound(new { message = "Room not found" });
        
        await roomRepository.DeleteAsync(roomId);
        return NoContent();
    }
}