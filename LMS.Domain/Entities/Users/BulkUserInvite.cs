using LMS.Domain.Entities.Auth;
using LMS.Domain.Entities.common;
using LMS.Domain.Enums.Users;

namespace LMS.Domain.Entities.Users;

public class BulkUserInvite : BaseEntity
{
    public int TenantId { get; set; }
    public string FileName { get; set; } = null!;
    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public BulkInvitedUserStatus Status { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }

    public Tenant Tenant { get; set; } = null!;
}