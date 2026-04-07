namespace LMS.Application.Features.Leaves.DTOs;

public record ApplyLeaveRequest(
    int LeaveTypeId,
    DateTime StartDate,
    DateTime EndDate,
    string? Reason
);