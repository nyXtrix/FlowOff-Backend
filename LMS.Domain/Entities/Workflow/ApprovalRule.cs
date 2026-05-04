using LMS.Domain.Entities.common;
using LMS.Domain.Enums.Policy;

namespace LMS.Domain.Entities.Workflow;

public class ApprovalRule : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; } = null!;
    public int Priority { get; set; }
    public ApprovalMode Mode { get; set; }
    public bool IsActive { get; set; } = true;
    public string ConditionJson { get; set; } = "[]";
    public ICollection<ApprovalStep> Steps { get; set; } = new List<ApprovalStep>();
}