using FluentValidation;
using LMS.Application.Common.Modals;
using LMS.Application.Features.Auth.DTOs;
using LMS.Application.Features.Auth.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LMS.Application.Common.Interfaces;
using LMS.Application.Features.Auth.Services;

namespace LMS.API.Controllers.Auth;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<RegisterCompanyRequest> _registerCompanyValidator;
    private readonly IValidator<ForgotPasswordRequest> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordRequest> _resetPasswordValidator;
    private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;
    private readonly ICacheService _cache;

    public AuthController(
        IAuthService authService, 
        IValidator<RegisterCompanyRequest> registerCompanyValidator,
        IValidator<ForgotPasswordRequest> forgotPasswordValidator,
        IValidator<ResetPasswordRequest> resetPasswordValidator,
        Microsoft.AspNetCore.Hosting.IWebHostEnvironment env,
        ICacheService cache)
    {
        _authService = authService;
        _registerCompanyValidator = registerCompanyValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
        _env = env;
        _cache = cache;
    }

    [HttpPost("register-company")]
    public async Task<IActionResult> RegisterCompany([FromBody] RegisterCompanyRequest request)
    {
        var validationResult = await _registerCompanyValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var fieldErrors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .Select(group => new ValidationErrorResponse
                {
                    ErrorCode = "VALIDATION_FAILED",
                    Field = group.Key,
                    Message = group.Count() == 1 
                        ? group.First().ErrorMessage 
                        : group.Select(e => e.ErrorMessage).ToList()
                })
                .ToList();

            return BadRequest(fieldErrors);
        }
        var response = await _authService.RegisterCompanyAsync(request);
        SetAuthCookie(response.Token);
        return Ok(response);
    }

    [Authorize]
    [HttpPost("invite-user")]
    public async Task<IActionResult> InviteUser([FromBody] InviteUserRequest request)
    {
        var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        var token = await _authService.InviteUserAsync(request, adminId);
        
        // In a real app, you would send an email here with the link:
        // https://{subdomain}.lms.com/set-password?token={token}
        
        return Ok(new { Message = "User invited successfully.", Token = token });
    }

    [HttpPost("set-password")]
    public async Task<IActionResult> SetPassword([FromBody] SetPasswordRequest request)
    {
        var response = await _authService.SetPasswordAsync(request);
        if (response == null) return BadRequest("Invalid or expired invite token.");
        
        SetAuthCookie(response.Token);
        return Ok(response);
    }

    [HttpGet("invite-details/{token}")]
    public async Task<IActionResult> GetInviteDetails(string token)
    {
        var details = await _authService.GetInviteDetailsAsync(token);
        return Ok(details);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var host = Request.Host.Host;
        string subdomain = host.Contains("localhost") || host == "127.0.0.1" 
                           ? Request.Headers["X-Tenant-Subdomain"].ToString() 
                           : host.Split('.')[0];

        if (string.IsNullOrEmpty(subdomain)) 
            return BadRequest(new ErrorResponse ( "MISSING_SUBDOMAIN", "Subdomain is required." ));

        var response = await _authService.LoginAsync(request, subdomain);
        SetAuthCookie(response.Token);
        return Ok(response);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetuCrrentUser()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _authService.GetCurrentUserAsync(userId);
        return Ok(user);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var token = Request.Cookies["AuthToken"] ?? 
                    Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

        if (!string.IsNullOrEmpty(token))
        {
            await _cache.SetAsync($"blacklisted_{token}", "revoked", TimeSpan.FromDays(7));
        }

        Response.Cookies.Delete("AuthToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = !_env.IsDevelopment(), // Matches SetAuthCookie
            SameSite = SameSiteMode.Strict
        });
        return Ok(new { Message = "Logged out successfully." });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var validationResult = await _forgotPasswordValidator.ValidateAsync(request);
        if (!validationResult.IsValid) return BadRequest(FormatValidationErrors(validationResult));

        var host = Request.Host.Host;
        string subdomain = host.Contains("localhost") || host == "127.0.0.1" 
                           ? Request.Headers["X-Tenant-Subdomain"].ToString() 
                           : host.Split('.')[0];

        if (string.IsNullOrEmpty(subdomain)) 
            return BadRequest(new ErrorResponse ( "MISSING_SUBDOMAIN", "Subdomain is required." ));

        await _authService.ForgotPasswordAsync(request, subdomain);
        return Ok(new { Message = "If an account exists for this email and subdomain, you will receive a reset link shortly." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var validationResult = await _resetPasswordValidator.ValidateAsync(request);
        if (!validationResult.IsValid) return BadRequest(FormatValidationErrors(validationResult));

        await _authService.ResetPasswordAsync(request);
        return Ok(new { Message = "Password reset successfully. You can now login with your new password." });
    }

    private List<ValidationErrorResponse> FormatValidationErrors(FluentValidation.Results.ValidationResult result)
    {
        return result.Errors
            .GroupBy(e => e.PropertyName)
            .Select(group => new ValidationErrorResponse
            {
                ErrorCode = "VALIDATION_FAILED",
                Field = group.Key,
                Message = group.Count() == 1 
                    ? group.First().ErrorMessage 
                    : group.Select(e => e.ErrorMessage).ToList()
            })
            .ToList();
    }

    private void SetAuthCookie(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !_env.IsDevelopment(), 
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        };

        Response.Cookies.Append("AuthToken", token, cookieOptions);
    }

    [AllowAnonymous]
    [HttpPost("contact")]
    public async Task<IActionResult>ContactUs([FromBody] ContactRequest request)
    {
        await _authService.SubmitContactInquiryAsync(request);
        return Ok(new {message = "Thank you! we have received your inquiry."});
    }
}
