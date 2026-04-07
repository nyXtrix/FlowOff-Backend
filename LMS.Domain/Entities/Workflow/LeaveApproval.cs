using LMS.Domain.Entities.Auth;
using LMS.Domain.Entities.common;
using LMS.Domain.Enums;

namespace LMS.Domain.Entities.Workflow;

public class LeaveApproval : BaseEntity
{
    public int LeaveRequestId { get; set; }
    public LeaveRequest LeaveRequest { get; set; } = null!;
    public int ApproverId { get; set; }
    public User Approver { get; set; } = null!;
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Waiting;
    public int Sequence { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Comments { get; set; }
}