namespace LMS.Domain.Enums.Users;

public enum BulkInvitedUserStatus
{
    Queued = 1,
    Processing,
    Completed,
    Failed,
    PartiallyFailed
}