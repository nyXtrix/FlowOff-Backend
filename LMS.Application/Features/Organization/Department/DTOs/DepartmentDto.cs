namespace LMS.Application.Features.Organization.Department.DTOs;

public record CreateDepartmentRequest(string Name, string? Description);
public record GetDepartments(string Label, Guid Value);