using System.Text.Json.Serialization;

namespace LMS.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LeaveStatus
{
    Pending, Approved, Rejected, Cancelled, InProgress
}

public enum ApprovalStatus
{
    Waiting, Pending, Approved, Rejected, Skipped
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApproverType
{
    MANAGER, SPECIFIC_USER, ROLE
}

public enum RoundingCondition
{
    Duration,
    LeaveType,
    Role,
    Balance,
    TeamAvailability,
    IsBackDated,
    IsBlackout
}

public enum EscalationAction
{
    Remind,
    Reassign,
    AutoApprove,
    SkipStep
}

public enum ApprovalMode
{
    AnyOne,
    All,
    Sequential
}