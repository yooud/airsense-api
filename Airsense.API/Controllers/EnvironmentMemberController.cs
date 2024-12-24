using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Airsense.API.Models.Dto;
using Airsense.API.Models.Dto.Environment;
using Airsense.API.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Airsense.API.Controllers;

[ApiController]
[Route("env/{envId:int}/member")]
[Authorize]
public class EnvironmentMemberController(
    IEnvironmentRepository environmentRepository,
    IUserRepository userRepository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetEnvironmentMembers(
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
        
        var members = await environmentRepository.GetMembersAsync(envId, count, skip);
        var totalCount = await environmentRepository.CountMembersAsync(envId);
        
        return Ok(new PaginatedListDto
        {
            Data = members,
            Pagination = new PaginatedListDto.Metadata
            {
                Skip = skip,
                Count = members.Count,
                Total = totalCount
            }
        });
    }
    
    [HttpPost("{email}")]
    public async Task<IActionResult> AddEnvironmentMember(int envId, string email)
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
        
        var user = await userRepository.GetByEmailAsync(email);
        if (user is null)
            return NotFound(new { message = "User not found" });

        var isMember = await environmentRepository.IsMemberAsync(user.Id, envId);
        if (isMember)
            return BadRequest(new { message = "User is already a member" });
        
        await environmentRepository.AddMemberAsync(envId, user.Id);
        return NoContent();
    }
    
    [HttpPost("{uid:int}")]
    public async Task<IActionResult> AddEnvironmentMemberById(int envId, int uid)
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

        var isMember = await environmentRepository.IsMemberAsync(uid, envId);
        if (isMember)
            return BadRequest(new { message = "User is already a member" });
        
        await environmentRepository.AddMemberAsync(envId, uid);
        return NoContent();
    }
    
    [HttpDelete("{uid:int}")]
    public async Task<IActionResult> RemoveEnvironmentMember(int envId, int uid)
    {
        if (!int.TryParse(User.FindFirstValue("id"), out var userId))
            return BadRequest(new { message = "You are not registered" });
        
        if (userId == uid)
            return BadRequest(new { message = "Cannot remove own member" });
        
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

        var isMember = await environmentRepository.IsMemberAsync(uid, envId);
        if (!isMember)
            return BadRequest(new { message = "User is not a member" });
        
        await environmentRepository.RemoveMemberAsync(envId, uid);
        return NoContent();
    }
    
    [HttpPatch("{uid:int}")]
    public async Task<IActionResult> UpdateEnvironmentMember(int envId, int uid, [FromBody] UpdateMemberRequestDto request)
    {
        if (!int.TryParse(User.FindFirstValue("id"), out var userId))
            return BadRequest(new { message = "You are not registered" });
        
        if (userId == uid)
            return BadRequest(new { message = "Cannot update own role" });
        
        var isExists = await environmentRepository.IsExistsAsync(envId);
        if (!isExists)
            return NotFound(new { message = "Environment not found" });
        
        var role = await environmentRepository.GetRoleAsync(userId, envId);
        switch (role)
        {
            case null:
            case "user":
                return Forbid();
            case "admin":
                if (request.Role.ToString().Equals("admin"))
                    return BadRequest(new { message = "Cannot update role to admin" });
                break;
        }

        var isMember = await environmentRepository.IsMemberAsync(uid, envId);
        if (!isMember)
            return BadRequest(new { message = "User is not a member" });
        
        await environmentRepository.UpdateMemberAsync(envId, uid, request.Role.ToString().ToLower());
        return NoContent();
    }
}