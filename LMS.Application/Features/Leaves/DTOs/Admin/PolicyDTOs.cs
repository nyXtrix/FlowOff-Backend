using LMS.Domain.Enums.Policy;

namespace LMS.Application.Features.Leaves.DTOs.Admin;

public record CreatePolicyRequest(
    string Name, 
    string ScopeType, 
    string ScopeValue, 
    int Priority, 
    WeekOffPolicyRequest Calendar, 
    UsagePolicyRequest Usage,
    List<ApprovalRuleRequest> ApprovalRules,
    List<BalancePolicyRequest> BalancePolicies
);

public record WeekOffPolicyRequest(List<WeekOffRuleDto> WeekOffs, bool AllowCompOff);

public record UsagePolicyRequest(
    bool SandwichEnabled,
    bool IncludeWeekendsInSandwich,
    bool IncludeHolidaysInSandwich,
    int AdvanceNoticeDays,
    bool AllowBackdated,
    int MaxFutureDays,
    int MinServiceDaysRequired,
    int MaxConsecutiveDays
);

public record ApprovalRuleRequest(
    string Name,
    int Priority,
    string ApprovalMode,
    object Conditions,
    List<ApprovalStepRequest> Steps
);

public record ApprovalStepRequest(
    int Order,
    string ApproverType,
    string? RoleId
);

public record BalancePolicyRequest(
    string LeaveTypeId,
    bool AllowCarryForward,
    decimal MaxCarryForward,
    bool AllowNegativeBalance,
    decimal MaxNegativeLimit,
    string RoundingRule
);

public record PolicySummaryDto(
    string Name,
    int ScopeType,
    string ScopeValue,
    int Priority,
    bool IsActive
);