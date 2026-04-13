using LMS.Application.Features.Auth.DTOs;
using LMS.Domain.Entities.Auth;

namespace LMS.Application.Features.Auth.Interfaces;

public interface IAuthenticationService
{
    Task<string> LoginAsync(LoginRequest request, string subdomain);
    Task<AuthResponse> GetCurrentUserAsync(Guid externalId);
    Task<string> CreateExchangeCodeAsync(string token);
    Task<string> ExchangeCodeAsync(string code);
    Task<IdentifyResponse> IdentifyUserAsync(string Email);
    string GenerateToken(User user, string subdomain);
}