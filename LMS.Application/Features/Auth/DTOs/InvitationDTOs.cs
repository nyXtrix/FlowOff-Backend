using LMS.Domain.Enums;

namespace LMS.Application.Features.Auth.DTOs;

public record InviteUserRequest(string Email, string FirstName, string LastName, Gender Gender, Guid RoleExternalId, Guid ManagerExternalId, Guid DepartmentExternalId);

public record SetPasswordRequest(string Token, string Password, string ConfirmPassword);

public record InviteDetailsResponse(string CompanyName, string Subdomain, string Id, string FirstName, string LastName, string Email);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string Password, string ConfirmPassword);
