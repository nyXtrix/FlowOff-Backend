using LMS.Domain.Entities.common;
using LMS.Domain.Enums.Policy;

namespace LMS.Domain.Entities.Leave;

public class LeaveAllocationPolicy : BaseEntity
{
    public int TenantId { get; set; }
    public int LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsAccrualBased { get; set; }
    public decimal AnnualEntitlement { get; set; }
    public AccuralFrequency AccuralFrequency { get; set; }
    public bool ProRateForNewJoiners { get; set; }
    public string? TenureBonusRulesJson { get; set; }
}