using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPOS.Application.Features.Auth.DTOs;
using SmartPOS.Application.Features.Auth.Interfaces;

namespace SmartPOS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        if (!response.Success)
        {
            return Unauthorized(response);
        }
        return Ok(response);
    }

    [HttpPost("verify-pin")]
    [Authorize]
    public async Task<IActionResult> VerifyPin([FromBody] ManagerPinVerifyRequest request)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var requestingUserId))
        {
            return Unauthorized("User is not authenticated.");
        }

        var response = await _authService.VerifyManagerPinAsync(request, requestingUserId);
        if (!response.IsAuthorized)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    [HttpPost("set-pin")]
    [Authorize]
    public async Task<IActionResult> SetPin([FromBody] string newPin)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized("User is not authenticated.");
        }

        var success = await _authService.SetUserPinAsync(userId, newPin);
        if (!success)
        {
            return BadRequest("Could not update manager PIN.");
        }
        return Ok(new { Success = true, Message = "PIN set successfully." });
    }
}
