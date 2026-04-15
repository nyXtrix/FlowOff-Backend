using LMS.Domain.Enums;

namespace LMS.Application.Features.Leaves.DTOs.Admin;

public record CreateLeaveTypeRequest(
    string Name,
    string? Description,
    int? MaxCancelableStep,
    int DefaultAnnualAllowence
);

public record LeaveTypeResponse(
    Guid ExternalId,
    string Name,
    string? Description,
    int? MaxCancelableStep,
    int DefaultAnnualAllowence
);

public record CreateHolidayRequest(string Name, DateTime Date);

public record HolidayResponse(
    Guid ExternalId,
    string Name,
    DateTime Date
);

public record CreateWorkflowRuleRequest(
    Guid? LeaveTypeExternalId,
    decimal MinDays,
    decimal MaxDays,
    List<CreateWorkflowStepDto> Steps
);

public record CreateWorkflowStepDto(
    int Sequence,
    ApproverType ApproverType,
    Guid? ApproverExternalId,
    Guid? RoleExternalId
);

public record WorkflowRuleResponse(
    Guid ExternalId,
    string? LeaveTypeName,
    decimal MinDays,
    decimal MaxDays,
    List<WorkflowStepResponse> Steps
);

public record WorkflowStepResponse(
    int Sequence,
    ApproverType ApproverType,
    string? ApproverName,
    string? RoleName
);
