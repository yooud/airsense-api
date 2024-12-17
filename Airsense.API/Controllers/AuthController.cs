using System.Security.Claims;
using Airsense.API.Models.Dto.Auth;
using Airsense.API.Models.Entity;
using Airsense.API.Repository;
using Airsense.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Airsense.API.Controllers;

[ApiController]
[Route("auth")]
[Authorize]
public class AuthController(
    IUserRepository userRepository,
    IAuthService authService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Login([FromBody] AuthRequestDto request)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        
        var isExists = await userRepository.IsExistsByUidAsync(uid);
        if (!isExists)
        {
            var user = new User
            {
                Name = User.FindFirstValue("name"),
                Email = User.FindFirstValue(ClaimTypes.Email),
                Uid = uid
            };
            user = await userRepository.CreateAsync(user);

            var result = await authService.SetIdAsync(uid, user.Id);
            if (!result)
                return StatusCode(500, new { Message = "Failed to set id" });
        }
        
        if (request.NotificationToken is not null)
            await userRepository.SetNotificationTokenAsync(uid, request.NotificationToken);

        return isExists ? Ok() : StatusCode(201);
    }
}