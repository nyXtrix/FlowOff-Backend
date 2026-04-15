using LMS.Domain.Module.Authorization;

namespace LMS.Application.Features.Auth.DTOs;

public record LoginRequest(string Email, string Password);
public record AuthResponse(
    string Id, 
    string FirstName,
    string LastName, 
    string Email, 
    string Subdomain, 
    LMS.Domain.Enums.Gender Gender, 
    LMS.Domain.Enums.UserStatus Status, 
    string Role, 
    AppPermissions Permissions,
    string TenantName
);
public record LoginResponse(string ExchangeCode);
public record ExchangeRequest(string Code);
public record IdentifyRequest(string Email);
public record IdentifyResponse(string? Subdomain, UserLoginStatus Status, bool Exists);
public enum UserLoginStatus
{
    Active = 1,
    Inactive = 2,
    SetPasswordRequired = 3,
    NotFound = 4
}