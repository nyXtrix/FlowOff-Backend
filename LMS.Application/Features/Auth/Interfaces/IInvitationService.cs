using LMS.Application.Features.Auth.DTOs;

namespace LMS.Application.Features.Auth.Interfaces;

public interface IInvitationService
{
    Task<string> InviteUserAsync(InviteUserRequest request, Guid inviterExternalId);

    Task<string> SetPasswordAsync(SetPasswordRequest request);
    Task<InviteDetailsResponse> GetInviteDetailsAsync(string token);
    Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request, string subdomain);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
}