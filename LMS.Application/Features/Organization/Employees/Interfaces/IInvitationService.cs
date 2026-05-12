using LMS.Application.Features.Organization.Employees.DTOs;

namespace LMS.Application.Features.Organization.Employees.Interfaces;

public interface IInvitationService
{
    Task<string> InviteUserAsync(InviteUserRequest request, Guid inviterExternalId);
    Task<string> SetPasswordAsync(SetPasswordRequest request);
    Task<InviteDetailsResponse> GetInviteDetailsAsync(string token);
    Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request, string subdomain);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
    Task<InviteDetailsResponse> VerifyResetTokenAsync(string token);
    Task ResendInvitationAsync(Guid userExternalId);
    Task CancelInvitationAsync(Guid userExternalId);
}
