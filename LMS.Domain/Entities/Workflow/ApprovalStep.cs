using LMS.Domain.Entities.common;
using LMS.Domain.Enums;

namespace LMS.Domain.Entities.Workflow;

public class ApprovalStep : BaseEntity
{
    public int ApprovalRuleId { get; set; }
    public int Sequence { get; set; }
    public ApproverType ApproverType { get; set; }
    public string? ApproverValue { get; set; }
    public int AutoApproveAfterDays { get; set; } = 0;
    public EscalationAction OnTimeout { get; set; }
}