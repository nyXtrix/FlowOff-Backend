using LMS.Domain.Enums;

namespace LMS.Application.Features.Leaves.DTOs;

public record CreateWorkflowRuleRequest(
    int? LeaveTypeId, 
    decimal MinDays, 
    decimal MaxDays, 
    List<WorkflowStepDto> Steps
);
public record WorkflowStepDto(
    int Sequence, 
    ApproverType ApproverType, 
    int ApproverId, 
    int? RoleId = null
);
public record WorkflowRuleResponse(
    int Id, 
    string? Name,
    int? LeaveTypeId, 
    string LeaveType, 
    decimal MinDays, 
    decimal MaxDays, 
    List<WorkflowStepDto> Steps
);