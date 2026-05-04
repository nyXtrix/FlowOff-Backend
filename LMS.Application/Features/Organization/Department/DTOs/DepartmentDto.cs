namespace LMS.Application.Features.Organization.Department.DTOs;

public record CreateDepartmentRequest(string Name, string? Description, bool IsActive = true);
public record UpdateDepartmentRequest(string? Name, string? Description, bool? IsActive);
public record DepartmentLookupResponse(string Label, Guid Value);

public record DepartmentManagementResponse(
    Guid Id,
    string DepartmentName,
    string? Description,
    int TotalEmployees,
    int LeaveCount,
    double LeavePercentage,
    List<string> LeavePreview
);
