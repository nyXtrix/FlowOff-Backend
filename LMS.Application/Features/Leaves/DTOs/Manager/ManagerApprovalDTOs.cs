namespace LMS.Application.Features.Leaves.DTOs.Manager;

public record ProcessApprovalRequest(
    Guid ApprovalExternalId,
    bool IsApproved,
    string? Remarks
);

public record PendingApprovalResponse(
    Guid ApprovalExternalId,
    Guid RequestExternalId,
    string EmployeeName,
    string LeaveType,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalDays,
    string Reason,
    DateTime AppliedAt
);

public record ForwardApprovalRequest(
    Guid ApprovalExternalId,
    Guid NewApproverExternalId,
    string? Remarks
);
