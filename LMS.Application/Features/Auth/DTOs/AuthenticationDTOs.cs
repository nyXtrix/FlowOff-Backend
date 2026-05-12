using LMS.Domain.Enums;
using LMS.Domain.Module.Authorization;

namespace LMS.Application.Features.Auth.DTOs;

public record LoginRequest(string Email, string Password);
public record AuthResponse(
    string Id, 
    string FirstName,
    string LastName, 
    string Email, 
    string Subdomain, 
    GenderEnum Gender, 
    UserStatus Status, 
    string Role, 
    string RoleCode,
    AppPermissions Permissions,
    string TenantName,
    DateTime CreatedTime
);
public record LoginResponse(string ExchangeCode);
public record ExchangeRequest(string Code);
public record IdentifyRequest(string Email);
public record IdentifyResponse(string? Subdomain, string? CompanyName, UserLoginStatus Status, bool Exists);
public enum UserLoginStatus
{
    Active = 1,
    Inactive = 2,
    SetPasswordRequired = 3,
    NotFound = 4
}