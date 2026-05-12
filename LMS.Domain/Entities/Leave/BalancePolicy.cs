using LMS.Domain.Entities.common;
using LMS.Domain.Enums.Policy;

namespace LMS.Domain.Entities.Leave;

public class BalancePolicy : BaseEntity
{
    public int TenantId { get; set; }
    public int LeaveTypeId { get; set; }
    public string Name { get; set; } = null!;
    public bool AllowCarryForward { get; set; }
    public decimal MaxCarryForwardLimit { get; set; }
    public int ExpiryDaysAfterYearEnd { get; set; } = 365;
    public bool AllowNegativeBalance { get; set; }
    public decimal MaxNegativeLimit { get; set; }
    public bool IsEncaseable { get; set; }
    public decimal MaxEncashmentLimit { get; set; }
    public RoundingRule RoundingRule { get; set; }

    public virtual LeaveType LeaveType { get; set; } = null!;
}