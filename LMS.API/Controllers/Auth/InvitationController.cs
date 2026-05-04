using System.Security.Claims;
using LMS.Application.Features.Auth.DTOs;
using LMS.Application.Features.Auth.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.API.Controllers.Auth;

[ApiController]
[Route("api/v1/auth/[controller]")]
public class InvitationController(IInvitationService invitationService) : ControllerBase
{
    [Authorize]
    [HttpPost("invite")]
    public async Task<IActionResult> InviteUser([FromBody] InviteUserRequest request)
    {
        var inviterExternalId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var token = await invitationService.InviteUserAsync(request, inviterExternalId);
        return Ok(new { inviteToken = token });
    }

    [HttpGet("invite-details")]
    public async Task<IActionResult> GetInviteDetails([FromQuery] string token)
    {
        var result = await invitationService.GetInviteDetailsAsync(token);
        return Ok(result);
    }

    [HttpPost("set-password")]
    public async Task<IActionResult> SetPassword([FromBody] SetPasswordRequest request)
    {
        var exchangeCode = await invitationService.SetPasswordAsync(request);
        return Ok(new LoginResponse(exchangeCode));
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var subdomain = Request.Headers["X-Tenant-Subdomain"].ToString();
        await invitationService.ForgotPasswordAsync(request, subdomain);
        return Ok(new { message = "If an account exists, a reset link has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await invitationService.ResetPasswordAsync(request);
        return Ok(new { message = "Password reset successful." });
    }

    [Authorize]
    [HttpPost("resend/{userExternalId}")]
    public async Task<IActionResult> ResendInvitation(Guid userExternalId)
    {
        await invitationService.ResendInvitationAsync(userExternalId);
        return Ok(new {message = "Invitation resent successfully"});
    }

    [Authorize]
    [HttpPost("cancel/{userExternalId}")]
    public async Task<IActionResult> CancelInvitation(Guid userExternalId)
    {
        await invitationService.CancelInvitationAsync(userExternalId);
        return Ok(new {message = "Invitation cancelled successfully"});
    }
}