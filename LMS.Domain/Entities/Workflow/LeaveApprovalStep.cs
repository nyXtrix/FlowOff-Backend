using LMS.Domain.Entities.Auth;
using LMS.Domain.Entities.common;
using LMS.Domain.Enums;

namespace LMS.Domain.Entities.Workflow;

public class LeaveApprovalStep : BaseEntity
{
    public int LeaveRequestId { get; set; }
    public LeaveRequest LeaveRequest { get; set; } = null!;

    public int? ApproverId { get; set; } 
    public User? Approver { get; set; }

    public int? RoleId { get; set; } 
    public Role? Role { get; set; }

    public ApprovalMode Mode { get; set; } = ApprovalMode.AnyOne;
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Waiting;
    public int StepOrder { get; set; }
    public DateTime? ActionDate { get; set; }
    public string? Comments { get; set; }
}