using LMS.Application.Features.Leaves.DTOs;
using LMS.Domain.Entities;
using LMS.Domain.Entities.Leave;
using LMS.Domain.Entities.Workflow;
using LMS.Domain.Enums;

namespace LMS.Application.Features.Leaves.Interfaces;

public interface IWorkflowEngine
{
    Task InitializeApprovalChainAsync(int LeaveRequestId);

    Task ProcessApprovalAsync(int approvalId, int approverId, bool isAprroved, string remarks);

    Task ForwardToApproverAsync(int approvalId, int currentApproverId, int nextApproverId);

    Task<LeaveStatus> GetChainStatusAsync(int LeaveRequestId);

    Task CancelLeaveRequestAsync(int leaveRequestId, int userId);

    Task<List<LeaveApproval>> GetPendingApprovalsAsync(int userId);

    Task<List<ApprovalHistoryDto>> GetApprovalHistoryAsync(int leaveRequestId);

    Task<List<LeaveRequest>> GetMyRequestsAsync(int userId);

    Task<List<LeaveBalance>> GetMyLeaveBalancesAsync(int userId);

    Task<List<LeaveType>> GetLeaveTypesAsync(int TenantId);

    Task<int> ApplyLeaveRequestAsync(ApplyLeaveRequest request, int userId, int tenantId);

    Task<int> CreateWorkflowRuleAsync(CreateWorkflowRuleRequest request, int tenantId);

    Task<int> DeleteWorkflowRuleAsync(int ruleId, int tenantId);

    Task<List<WorkflowRuleResponse>> GetWorkflowRuleResponsesAsync(int tenantId);

    Task<int> CreateHolidayAsync(CreateHolidayRequest request, int tenantId);

    Task DeleteHolidayRequestAsync(int holidayId, int tenantId);

    Task<List<HolidayResponse>> GetHolidaysAsync(int tenantId);

    Task<int> CreateLeaveTypeAsync(CreateLeaveTypeRequest request, int tenantId);
    
}
