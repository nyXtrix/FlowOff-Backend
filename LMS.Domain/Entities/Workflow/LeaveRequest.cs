using LMS.Domain.Entities.common; 
using LMS.Domain.Entities.Auth;   
using LMS.Domain.Entities.Leave; 
using LMS.Domain.Enums;


namespace LMS.Domain.Entities.Workflow;

public class LeaveRequest : BaseEntity
{
    public int TenantId { get; set; }
    public int UserId { get; set; }
    public int LeaveTypeId { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalDays { get; set; } 
    
    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
    public string? Reason { get; set; }
    public string? RejectionReason { get; set; }

    public User User { get; set; } = null!;
    public LeaveType LeaveType { get; set; } = null!;

    public ICollection<LeaveApprovalStep> ApprovalSteps { get; set; } = new List<LeaveApprovalStep>();
}
