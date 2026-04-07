using LMS.Domain.Entities.common;
using LMS.Domain.Enums;

namespace LMS.Domain.Entities.Workflow;

public class WorkflowStep : BaseEntity
{
    public int WorkflowRuleId { get; set; }

    public int Sequence { get; set; }

    public ApproverType ApproverType { get; set; }

    public int ApproverId { get; set; }

    public int? RoleId { get; set; }
}