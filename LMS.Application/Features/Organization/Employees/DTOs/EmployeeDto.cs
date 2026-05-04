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
    UserStatus Status,
    DateTime CreatedAt
);