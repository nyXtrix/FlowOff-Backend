namespace LMS.Domain.Enums;

public enum LeaveStatus
{
    Pending, Approved, Rejected, Cancelled
}

public enum ApprovalStatus
{
    Waiting, Pending, Approved, Rejected, Skipped
}

public enum ApproverType
{
    Manager, SpecificUser, Role
}