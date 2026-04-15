namespace LMS.Application.Features.Leaves.DTOs.Employee;

public record ApplyLeaveRequest(
    Guid LeaveTypeExternalId,
    DateTime StartDate,
    DateTime EndDate,
    string? Reason
);

public record MyLeaveRequestResponse(
    Guid ExternalId,
    string LeaveType,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalDays,
    string Status,
    DateTime CreatedAt
);

public record LeaveBalanceResponse(
    Guid LeaveTypeExternalId,
    string LeaveType,
    decimal Balance,
    int Year
);
