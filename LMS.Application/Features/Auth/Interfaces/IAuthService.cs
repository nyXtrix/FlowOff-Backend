        using LMS.Application.Features.Auth.DTOs;
using LMS.Domain.Entities.Auth;

namespace LMS.Application.Features.Auth.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterCompanyAsync(RegisterCompanyRequest request);
    Task<string> InviteUserAsync(InviteUserRequest request, int adminUserId);
    Task<AuthResponse?> SetPasswordAsync(SetPasswordRequest request);
    Task<InviteDetailsResponse> GetInviteDetailsAsync(string token);
    Task<AuthResponse> LoginAsync(LoginRequest request, string subdomain);
    Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request, string subdomain);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
    Task<AuthResponse> GetCurrentUserAsync(int userId);

    Task SubmitContactInquiryAsync(ContactRequest request);
}
