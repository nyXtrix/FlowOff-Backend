using LMS.Domain.Enums;

namespace LMS.Application.Features.Employees.DTOs;

public record EmployeeLookupResponse(string Label, Guid Value, string Role);

public record EmployeeListResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string DepartmentName,
    string RoleName,
    int Status,
    DateTime CreatedAt
);

public class UpdateEmployeeRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int Gender { get; set; }
    public string? DepartmentId { get; set; }
    public string? ManagerId { get; set; }
    public string? RoleId { get; set; }
}