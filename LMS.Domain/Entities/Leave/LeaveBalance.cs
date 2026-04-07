using LMS.Domain.Entities.common;

namespace LMS.Domain.Entities.Leave;

public class LeaveBalance : BaseEntity
{
    public int TenantId { get; set; }

    public int UserId { get; set; }

    public LeaveType LeaveType { get; set; } = null!;

    public int LeaveTypeId { get; set; }

    public decimal Balance { get; set; }

    public int Year { get; set; }
}