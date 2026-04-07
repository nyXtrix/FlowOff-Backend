using LMS.Domain.Entities.common;

namespace LMS.Domain.Entities.Leave;

public class LeaveType : BaseEntity
{
    public int TenantId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int DefaultAnnualAllowence { get; set; }

    public int? MaxCancelableStep { get; set; }
}