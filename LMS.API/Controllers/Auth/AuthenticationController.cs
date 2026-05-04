using Microsoft.AspNetCore.Mvc;
using LMS.Application.Features.Auth.Interfaces;
using LMS.Application.Features.Auth.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using LMS.Application.Common.Interfaces;

namespace LMS.API.Controllers.Auth;

[ApiController]
[Route("api/v1/auth/[controller]")]
public class AuthenticationController(IAuthenticationService authService, ICacheService cache) : ControllerBase
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
            SameSite = SameSiteMode.None,
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

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        try
        {
            var newToken = await authService.RefreshTokenAsync();
            
            Response.Cookies.Append("AuthToken", newToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return Ok(new { message = "Token refreshed successfully" });
        }
        catch
        {
            Response.Cookies.Delete("AuthToken", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None
            });
            return Unauthorized();
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userExternalId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userExternalId != null)
        {
            await cache.RemoveAsync($"upr_{userExternalId}");
        }

        Response.Cookies.Delete("AuthToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None
        });

        return Ok(new { message = "User logged out successfully" });
    }
}