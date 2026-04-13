using Microsoft.AspNetCore.Mvc;
using LMS.Application.Features.Auth.Interfaces;
using LMS.Application.Features.Auth.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace LMS.API.Controllers.Auth;

[ApiController]
[Route("api/[controller]")]
public class AuthenticationController(IAuthenticationService authService) : ControllerBase
{
    [HttpPost("identify")]
    [AllowAnonymous]
    public async Task<IActionResult> Identify([FromBody] IdentifyRequest request)
    {
        var result = await authService.IdentifyUserAsync(request.Email);
        return Ok(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var subdomain = Request.Headers["X-Tenant-Subdomain"].ToString();
        if (string.IsNullOrEmpty(subdomain)) return BadRequest("Tenant header missing");

        var exchangeCode = await authService.LoginAsync(request, subdomain);
        return Ok(new LoginResponse(exchangeCode));
    }

    [HttpPost("exchange")]
    [AllowAnonymous]
    public async Task<IActionResult> ExchangeCode([FromBody] ExchangeRequest request)
    {
        var token = await authService.ExchangeCodeAsync(request.Code);
        Response.Cookies.Append("AuthToken", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddDays(7)
        });

        return Ok(new { message = "Login successful" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userExternalId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

        var user = await authService.GetCurrentUserAsync(userExternalId);

        return Ok(user);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        Response.Cookies.Delete("AuthToken");

        return Ok(new { message = "User logged out successfully" });
    }
}