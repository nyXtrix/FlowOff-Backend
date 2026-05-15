using LMS.Domain.Entities.common;
using LMS.Domain.Enums;

namespace LMS.Domain.Entities.Workflow;

public class WorkflowStep : BaseEntity
{
    public int WorkflowRuleId { get; set; }
    public int Sequence { get; set; }
    public ApproverType ApproverType { get; set; }
    public string? ApproverValue { get; set; }
    public int? ApproverId { get; set; }
    public int? RoleId { get; set; }
    public int AutoApproveAfterDays { get; set; } = 0;
    public EscalationAction OnTimeout { get; set; }

    public virtual Auth.User? Approver { get; set; }
    public virtual Role? Role { get; set; }
    public virtual WorkflowRule WorkflowRule { get; set; } = null!;
}