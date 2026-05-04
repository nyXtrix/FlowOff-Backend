using LMS.Domain.Entities.common;

namespace LMS.Domain.Entities.Leave;

public class WeekOffPolicy : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; } = null!;
    public string RulesJson { get; set; } = "[]";
    public bool IsRotational { get; set; }
    public string? RotationalGroupId { get; set; }
    public bool AllowCompOff { get; set; }
}