using LMS.Domain.Enums;

namespace LMS.Application.Features.Employees.DTOs;

public class EmployeeProfileResponse
{
    public EmployeeProfileDto Employee { get; set; } = new();
    public List<LeaveBalanceDetailDto> LeaveBalances { get; set; } = new();
    public RequestStatusCountsDto RequestStatusCounts { get; set; } = new();
    public List<Dictionary<string, object>> LeaveHistory { get; set; } = new();
}

public class EmployeeProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Gender { get; set; }
    public int Status { get; set; }
    public string Role { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? DepartmentId { get; set; }
    public string? ManagerName { get; set; }
    public string? ManagerId { get; set; }
}

public class LeaveBalanceDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public decimal Used { get; set; }
    public decimal Available { get; set; }
    public int Pending { get; set; }
}

public class RequestStatusCountsDto
{
    public int Total { get; set; }
    public int Pending { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public int Cancelled { get; set; }
}
