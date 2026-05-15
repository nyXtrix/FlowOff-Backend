namespace LMS.Domain.Entities.Workflow;

using LMS.Domain.Enums;
using LMS.Domain.Entities.common;
using LMS.Domain.Entities.Leave;

public class WorkflowRule : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; } = null!;
    public int? LeaveTypeId { get; set; }
    public virtual LeaveType LeaveType { get; set; } = null!;

    public decimal MinDays { get; set; }
    public decimal MaxDays { get; set; }
    public bool CancelLeaveAnyTime { get; set; }
    public int? DisableCancelAfterStep { get; set; }
    
    public int Priority { get; set; }
    public LMS.Domain.Enums.Policy.WorkflowApprovalMode Mode { get; set; }
    public bool IsActive { get; set; } = true;
    public string ConditionJson { get; set; } = "[]";

    public virtual ICollection<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();
}
