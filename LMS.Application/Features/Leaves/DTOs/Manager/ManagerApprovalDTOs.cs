using LMS.Domain.Enums;

namespace LMS.Application.Features.Leaves.DTOs.Manager;

public record ProcessApprovalRequest(
    Guid ApprovalExternalId,
    bool IsApproved,
    string? Remarks
);

public record ApprovalListResponse(
    Guid ApprovalExternalId,
    Guid RequestExternalId,
    string EmployeeName,
    string LeaveType,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalDays,
    string Reason,
    DateTime AppliedAt,
    ApprovalStatus Status
);

public record ForwardApprovalRequest(
    Guid ApprovalExternalId,
    Guid NewApproverExternalId,
    string? Remarks
);

public record StatCardDto(
    string Type,
    string Title,
    string Value,
    string Subtitle
);

public record ApprovalStatsResponse(
    List<StatCardDto> StatCards
);
