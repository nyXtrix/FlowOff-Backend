using LMS.Domain.Entities.common;

namespace LMS.Domain.Entities.Leave;

public class LeaveUsagePolicy : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; } = null!;
    public bool SandwichEnabled { get; set; }
    public bool IncludeWeekendsInSandwich { get; set; }
    public bool IncludeHolidaysInSandwich { get; set; }
    public int AdvanceNoticeDays { get; set; }
    public bool AllowBackdated { get; set; }
    public int MaxFutureDays { get; set; }
    public int MinServiceDaysRequired { get; set; }
    public int MaxConsecutiveDays { get; set; }
    public bool AllowPartialDays { get; set; }
    public string? BlackoutPeriodJson { get; set; }
    public string? ClubbingRulesJson { get; set; }
}