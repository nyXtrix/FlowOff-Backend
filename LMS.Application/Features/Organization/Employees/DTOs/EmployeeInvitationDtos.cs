using LMS.Domain.Entities.Users;
using LMS.Domain.Enums;
using LMS.Domain.Enums.Users;

namespace LMS.Application.Features.Organization.Employees.DTOs;

public record InviteUserRequest(string Email, string FirstName, string LastName, GenderEnum Gender, Guid RoleExternalId, Guid ManagerExternalId, Guid DepartmentExternalId);

public record SetPasswordRequest(string Token, string Password, string ConfirmPassword);

public record InviteDetailsResponse(string CompanyName, string Subdomain, string Id, string FirstName, string LastName, string Email);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string Password, string ConfirmPassword);
public record BulkUserInvitedStatus(int TotalRows, int ProcessedRows, int SuccessCount, int FailureCount, int Status, Guid ExternalId, string? ErrorMessage = null);

public record BulkUserInviteDto
{
    public Guid ExternalId { get; set; }
    public string FileName { get; set; } = null!;
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

public record BulkUserInviteDetailsDto
{
    public Guid ExternalId { get; set; }
    public string FileName { get; set; } = null!;
    public int Status { get; set; }
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<BulkRowResultDto> Results { get; set; } = new();
}

public record BulkRowResultDto
{
    public string Email { get; set; } = null!;
    public int Status { get; set; }
    public string? ErrorMessage { get; set; }
}
