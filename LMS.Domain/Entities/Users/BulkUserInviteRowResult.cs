using LMS.Domain.Entities.common;

namespace LMS.Domain.Entities.Users;

public class BulkUserInviteRowResult : BaseEntity
{
    public int BulkUserInviteId { get; set; }
    public string Email { get; set; } = null!;
    public BulkRowStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    
    public BulkUserInvite BulkUserInvite { get; set; } = null!;
}

public enum BulkRowStatus
{
    Failed = 1,
    Skipped = 2
}
