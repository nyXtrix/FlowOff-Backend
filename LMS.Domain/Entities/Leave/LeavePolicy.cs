using LMS.Domain.Entities.common;
using LMS.Domain.Enums.Policy;

namespace LMS.Domain.Entities.Leave;

public class LeavePolicy : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; } = null!;
    public LeavePolicyType Type { get; set; }
    public string ConfigJson { get; set; } = "{}";
    
    public int? LeaveTypeId { get; set; }
    public virtual LeaveType? LeaveType { get; set; }
}
