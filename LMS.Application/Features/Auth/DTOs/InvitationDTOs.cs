using LMS.Domain.Enums;

namespace LMS.Application.Features.Auth.DTOs;

public record InviteUserRequest(string Email, string Name, Gender Gender, Guid RoleExternalId, Guid PositionExternalId, Guid ManagerExternalId);

public record SetPasswordRequest(string Token, string Password, string ConfirmPassword);

public record InviteDetailsResponse(string CompanyName, string Subdomain, string Id, string Name, string Email);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string Password, string ConfirmPassword);