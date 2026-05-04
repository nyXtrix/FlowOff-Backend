namespace LMS.Domain.Enums;

public enum LeaveStatus
{
    Pending, Approved, Rejected, Cancelled, InProgress
}

public enum ApprovalStatus
{
    Waiting, Pending, Approved, Rejected, Skipped
}

public enum ApproverType
{
    Manager, SpecificUser, Role
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