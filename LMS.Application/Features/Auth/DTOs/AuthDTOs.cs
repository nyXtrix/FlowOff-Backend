using LMS.Domain.Enums;

namespace LMS.Application.Features.Auth.DTOs;

public record RegisterCompanyRequest(string RegistrationToken, string CompanyName, string Subdomain, string AdminName, string AdminEmail, string AdminPassword);
public record LoginRequest(string Email, string Password);
public record SetPasswordRequest(string Token, string Password);
public record InviteUserRequest(string Email, string Name);
public record AuthResponse(string Token, string Name, string Email, string Subdomain);
public record InviteDetailsResponse(string CompanyName, string Name, string Email);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string Password);
public record FinalizeRegistrationRequest(string Token, string Subdomain, string Password);
public record ContactRequest(string Name, string Email, string Message, InquiryPurpose Purpose);