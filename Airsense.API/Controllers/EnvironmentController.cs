using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Airsense.API.Models.Dto;
using Airsense.API.Models.Dto.Environment;
using Airsense.API.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Environment = Airsense.API.Models.Entity.Environment;

namespace Airsense.API.Controllers;

[ApiController]
[Route("env")]
[Authorize]
public class EnvironmentController(IEnvironmentRepository environmentRepository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAvailableEnvironments(
        [FromQuery][Range(0, int.MaxValue, ErrorMessage = "The skip parameter must be a non-negative integer.")] int skip = 0,
        [FromQuery][Range(0, int.MaxValue, ErrorMessage = "The count parameter must be a non-negative integer.")] int count = 10
    )
    {
        if (!int.TryParse(User.FindFirstValue("id"), out var userId))
            return BadRequest(new { message = "You are not registered" });

        var environments = await environmentRepository.GetAvailableAsync(userId, count, skip);
        var totalCount = await environmentRepository.CountAvailableAsync(userId);
        
        return Ok(new PaginatedListDto
            {
                Data = environments,
                Pagination = new PaginatedListDto.Metadata
                {
                    Skip = skip,
                    Count = environments.Count,
                    Total = totalCount
                }
            }
        );  
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateEnvironment([FromBody] CreateEnvironmentRequestDto request)
    {
        if (!int.TryParse(User.FindFirstValue("id"), out var userId))
            return BadRequest(new { message = "You are not registered" });

        var environment = new Environment
        {
            Name = request.Name
        };
        environment = await environmentRepository.CreateAsync(environment, userId);
        return Created(nameof(GetEnvironment), new { environment.Id });
    }
    
    [HttpGet("{envId:int}")]
    public async Task<IActionResult> GetEnvironment(int envId)
    {
        if (!int.TryParse(User.FindFirstValue("id"), out var userId))
            return BadRequest(new { message = "You are not registered" });

        var environment = await environmentRepository.GetByIdAsync(envId, userId);
        if (environment is null)
            return NotFound(new { message = "Environment not found" });
        
        if (!await environmentRepository.IsMemberAsync(userId, envId))
            return Forbid();
        
        return Ok(environment);
    }

    [HttpDelete("{envId:int}")]
    public async Task<IActionResult> DeleteEnvironment(int envId)
    {
        if (!int.TryParse(User.FindFirstValue("id"), out var userId))
            return BadRequest(new { message = "You are not registered" });

        var environment = await environmentRepository.GetByIdAsync(envId, userId);
        if (environment is null)
            return NotFound(new { message = "Environment not found" });
        
        var role = await environmentRepository.GetRoleAsync(userId, envId);
        if (role is null)
            return Forbid();

        if (!role.Equals("owner"))
            return Forbid();

        await environmentRepository.DeleteAsync(envId);
        return NoContent();
    }

    [HttpPatch("{envId:int}")]
    public async Task<IActionResult> UpdateEnvironment(int envId, [FromBody] UpdateEnvironmentRequestDto request)
    {
        if (!int.TryParse(User.FindFirstValue("id"), out var userId))
            return BadRequest(new { message = "You are not registered" });

        var environment = await environmentRepository.GetByIdAsync(envId, userId);
        if (environment is null)
            return NotFound(new { message = "Environment not found" });
        
        var role = await environmentRepository.GetRoleAsync(userId, envId);
        if (role is null)
            return Forbid();

        if (!role.Equals("owner"))
            return Forbid();
        
        await environmentRepository.UpdateAsync(envId, request.Name);
        return NoContent();
    }
}