using LMS.Domain.Entities.common;
using LMS.Domain.Enums.Policy;
namespace LMS.Domain.Entities.Organization;

public class PolicyScopes : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; } = null!;
    public PolicyScope ScopeType { get; set; }
    public string ScopeValue { get; set; } = null!;
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;
    public int? WeekOffPolicyId { get; set; }
    public int? UsagePolicyId { get; set; }
    public int? BalancePolicyId { get; set; }
}