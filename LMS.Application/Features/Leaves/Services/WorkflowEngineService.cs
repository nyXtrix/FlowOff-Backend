using LMS.Application.Common.Interfaces;
using LMS.Application.Common.Modals;
using LMS.Application.Features.Leaves.DTOs;
using LMS.Application.Features.Leaves.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Entities.Leave;
using LMS.Domain.Entities.Workflow;
using LMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Leaves.Services;

public class WorkflowEngineService(IAppDbContext context) : IWorkflowEngine
{

    public async Task InitializeApprovalChainAsync(int leaveRequestId)
    {
        var request = await context.LeaveRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == leaveRequestId)
            ?? throw new AppException(404, "Request not found", "NOT_FOUND");

        request.TotalDays = await CalculateActualWorkingDaysAsync(request.TenantId, request.StartDate, request.EndDate);

        var rule = await context.WorkflowRules.Include(r => r.Steps).Where(r => r.TenantId == request.TenantId
            && (r.LeaveTypeId == null || r.LeaveTypeId == request.LeaveTypeId) && request.TotalDays >= r.MinDays
            && (r.MaxDays == 0 || request.TotalDays <= r.MaxDays)).OrderByDescending(r => r.LeaveTypeId).FirstOrDefaultAsync();

        if (rule == null || rule.Steps.Count == 0)
        {
            throw new AppException(400, "No approval workflow defined for this leave duration. Contact Admin", "NO_WORKFLOW_DEFINED");
        }

        var oldApprovals = context.LeaveApprovals.Where(a => a.LeaveRequestId == leaveRequestId);
        context.LeaveApprovals.RemoveRange(oldApprovals);

        foreach (var step in rule.Steps.OrderBy(s => s.Sequence))
        {
            var approverId = await ResolveApproverId(step, request.UserId);

            context.LeaveApprovals.Add(new LeaveApproval
            {
                LeaveRequestId = leaveRequestId,
                Sequence = step.Sequence,
                ApproverId = approverId,
                Status = step.Sequence == 1 ? ApprovalStatus.Pending : ApprovalStatus.Waiting
            });

        }
    }

    private async Task<int> ResolveApproverId(WorkflowStep step, int requesterId)
    {
        if (step.ApproverType == ApproverType.Manager)
        {
            var user = await context.Users.FindAsync(requesterId);

            if (user?.ManagerId == null) throw new AppException(400, "Manager not found. Update your profile before applying", "MANAGER_NOT_FOUND");

            return user.ManagerId.Value;
        }

        if (step.ApproverType == ApproverType.SpecificUser) return (int?)step.ApproverId ?? throw new AppException(400, "Invalid rule: Specific User ID missing", "RULE_CONFIG_ERROR");

        throw new AppException(501, "Role-based approval is comming soon", "NOT_IMPLEMENTED");
    }

    public async Task<int> CreateLeaveTypeAsync(CreateLeaveTypeRequest request, int tenantId)
    {

        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var leaveType = new LeaveType
            {
                TenantId = tenantId,
                Name = request.Name,
                Description = request.Description,
                MaxCancelableStep = request.MaxCancelableStep,
                DefaultAnnualAllowence = request.DefaultAnnualAllowence
            };

            context.LeaveTypes.Add(leaveType);
            await context.SaveChangesAsync();

            var employees = await context.Users.Where(u => u.TenantId == tenantId).ToListAsync();

            foreach (var emp in employees)
            {
                context.LeaveBalances.Add(new LeaveBalance
                {
                    TenantId = tenantId,
                    UserId = emp.Id,
                    LeaveTypeId = leaveType.Id,
                    Balance = leaveType.DefaultAnnualAllowence,
                    Year = DateTime.Now.Year
                });
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return leaveType.Id;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }



    public async Task ProcessApprovalAsync(int approvalId, int approverId, bool isApproved, string remarks)
    {
        var currentStep = await context.LeaveApprovals.Include(a => a.LeaveRequest).FirstOrDefaultAsync(a => a.Id == approvalId) ?? throw new AppException(404, "Approval step not found", "NOT_FOUND");

        if (currentStep.ApproverId != approverId) throw new AppException(403, "You are not authorized to approve this step.", "FORBIDDEN");

        if (!isApproved)
        {
            currentStep.Status = ApprovalStatus.Rejected;
            currentStep.Comments = remarks;
            currentStep.LeaveRequest.Status = LeaveStatus.Rejected;
            currentStep.LeaveRequest.RejectionReason = remarks;
        }
        else
        {
            currentStep.Status = ApprovalStatus.Approved;
            currentStep.Comments = remarks;

            var nextStep = await context.LeaveApprovals.Where(a => a.LeaveRequestId == currentStep.LeaveRequestId && a.Sequence == currentStep.Sequence + 1).FirstOrDefaultAsync();

            if (nextStep == null)
            {
                currentStep.LeaveRequest.Status = LeaveStatus.Approved;

                var balance = await context.LeaveBalances.FirstOrDefaultAsync(b => b.UserId == currentStep.LeaveRequest.UserId && b.LeaveTypeId == currentStep.LeaveRequest.LeaveTypeId && b.Year == DateTime.Now.Year);

                if (balance?.Balance < currentStep.LeaveRequest.TotalDays)
                {
                    throw new AppException(400, "Insufficient leave balance", "INSUFFICIENT_BALANCE");
                }

                else if (balance != null)
                {
                    balance.Balance -= currentStep.LeaveRequest.TotalDays;
                }
            }
            else
            {
                nextStep.Status = ApprovalStatus.Pending;
            }
        }
        await context.SaveChangesAsync();
    }

    public async Task ForwardToApproverAsync(int approvalId, int currentApproverId, int nextApproverId)
    {
        var currentStep = await context.LeaveApprovals.Include(a => a.LeaveRequest)
         .FirstOrDefaultAsync(a => a.Id == approvalId) ?? throw new AppException(404, "Step not found", "NOT_FOUND");

        if (currentStep.ApproverId != currentApproverId) throw new AppException(403, "Not authorized", "FORBIDDEN");

        currentStep.Status = ApprovalStatus.Approved;
        currentStep.Comments = "Forwarded to next level approval";


        var subSequentSteps = await context.LeaveApprovals.Where(a => a.LeaveRequestId == currentStep.LeaveRequestId && a.Sequence > currentStep.Sequence).ToListAsync();

        foreach (var step in subSequentSteps)
        {
            step.Sequence += 1;
        }

        context.LeaveApprovals.Add(new LeaveApproval
        {
            LeaveRequestId = currentStep.LeaveRequestId,
            ApproverId = nextApproverId,
            Sequence = currentStep.Sequence + 1,
            Status = ApprovalStatus.Pending,
            Comments = "Manually added by previous approver"
        });

        await context.SaveChangesAsync();
    }

    public async Task<LeaveStatus> GetChainStatusAsync(int LeaveRequestId)
    {
        var request = await context.LeaveRequests.FindAsync(LeaveRequestId);
        return request?.Status ?? LeaveStatus.Pending;
    }

    public async Task CancelLeaveRequestAsync(int leaveRequestId, int userId)
    {
        var request = await context.LeaveRequests.Include(r => r.LeaveType).Include(r => r.Approvals)
         .FirstOrDefaultAsync(r => r.Id == leaveRequestId && r.UserId == userId) ?? throw new AppException(404, "Leave Request not found", "NOT_FOUND");

        var limit = request.LeaveType.MaxCancelableStep;

        if (request.Approvals.Any(a => a.Sequence > limit && a.Status == ApprovalStatus.Approved))
        {
            throw new AppException(400, "Cancellation blocked: This leave already passed the max level", "CANCEL_BLOCKED");
        }


        if (request.Status == LeaveStatus.Approved)
        {
            var balance = await context.LeaveBalances.FirstOrDefaultAsync(b => b.UserId == userId && b.LeaveTypeId == request.LeaveTypeId && b.Year == DateTime.Now.Year);
            if (balance != null)
            {
                balance.Balance += request.TotalDays;
            }
        }

        request.Status = LeaveStatus.Cancelled;

        foreach (var app in request.Approvals.Where(a => a.Status == ApprovalStatus.Pending || a.Status == ApprovalStatus.Waiting))
        {
            app.Status = ApprovalStatus.Skipped;
        }

        await context.SaveChangesAsync();
    }

    public async Task<List<LeaveApproval>> GetPendingApprovalsAsync(int userId)
    {
        return await context.LeaveApprovals.Include(a => a.LeaveRequest)
         .ThenInclude(r => r.User).Include(a => a.LeaveRequest).ThenInclude(r => r.LeaveType)
         .Where(a => a.ApproverId == userId && a.Status == ApprovalStatus.Pending)
         .OrderByDescending(a => a.LeaveRequest.CreatedAt).ToListAsync();
    }

    public async Task<List<ApprovalHistoryDto>> GetApprovalHistoryAsync(int leaveRequestId)
    {
        return await context.LeaveApprovals.Include(a => a.Approver).Where(a => a.LeaveRequestId == leaveRequestId)
          .OrderBy(a => a.Sequence).Select(a => new ApprovalHistoryDto(a.Sequence, a.Approver.Name, a.Status.ToString(), a.Comments, a.ApprovedAt)).ToListAsync();
    }

    public async Task<List<LeaveRequest>> GetMyRequestsAsync(int userId)
    {
        return await context.LeaveRequests.Include(r => r.LeaveType).Where(r => r.UserId == userId).OrderByDescending(r => r.CreatedAt).ToListAsync();
    }

    public async Task<List<LeaveBalance>> GetMyLeaveBalancesAsync(int userId)
    {
        return await context.LeaveBalances.Include(b => b.LeaveType).Where(r => r.UserId == userId).ToListAsync();
    }

    public async Task<List<LeaveType>> GetLeaveTypesAsync(int TenantId)
    {
        return await context.LeaveTypes.Where(t => t.TenantId == TenantId).ToListAsync();
    }

    public async Task<int> ApplyLeaveRequestAsync(ApplyLeaveRequest request, int userId, int tenantId)
    {
        var leaveRequest = new LeaveRequest
        {
            UserId = userId,
            TenantId = tenantId,
            LeaveTypeId = request.LeaveTypeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Reason = request.Reason,
            Status = LeaveStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            TotalDays = await CalculateActualWorkingDaysAsync(tenantId, request.StartDate, request.EndDate)
        };

        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            context.LeaveRequests.Add(leaveRequest);
            await context.SaveChangesAsync();

            await InitializeApprovalChainAsync(leaveRequest.Id);

            await context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return leaveRequest.Id;
    }
    private async Task<decimal> CalculateActualWorkingDaysAsync(int tenantId, DateTime start, DateTime end)
    {
        var tenant = await context.Tenants.FindAsync(tenantId);

        var weekOffs = tenant?.WeekoffDays?.Split(',').Select(int.Parse).ToList() ?? [];

        var holidays = await context.Holidays.Where(h => h.TenantId == tenantId && h.Date >= start && h.Date <= end).Select(h => h.Date.Date).ToListAsync();

        decimal totalDays = 0;
        for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
        {
            if (weekOffs.Contains((int)date.DayOfWeek) || holidays.Contains(date))
            {
                continue;
            }
            totalDays++;
        }
        return totalDays;
    }

    public async Task<int> CreateWorkflowRuleAsync(CreateWorkflowRuleRequest request, int tenantId)
    {
        var rule = new WorkflowRule
        {
            TenantId = tenantId,
            LeaveTypeId = request.LeaveTypeId,
            MinDays = request.MinDays,
            MaxDays = request.MaxDays
        };

        foreach (var stepDto in request.Steps.OrderBy(s => s.Sequence))
        {
            rule.Steps.Add(new WorkflowStep
            {
                Sequence = stepDto.Sequence,
                ApproverType = stepDto.ApproverType,
                ApproverId = stepDto.ApproverId,
                RoleId = stepDto.RoleId
            });
        }

        context.WorkflowRules.Add(rule);
        await context.SaveChangesAsync();

        return rule.Id;
    }

    public async Task<List<WorkflowRuleResponse>> GetWorkflowRuleResponsesAsync(int tenantId)
    {
        return await context.WorkflowRules
            .Include(r => r.Steps)
            .Include(r => r.LeaveType)
            .Where(r => r.TenantId == tenantId)
            .Select(r => new WorkflowRuleResponse(
                r.Id,
                r.Name,
                r.LeaveTypeId,
                r.LeaveType != null ? r.LeaveType.Name : "General (All)",
                r.MinDays,
                r.MaxDays,
                r.Steps.Select(s => new WorkflowStepDto(s.Sequence, s.ApproverType, s.ApproverId, s.RoleId)).ToList()
            ))
            .ToListAsync();
    }


    public async Task<int> DeleteWorkflowRuleAsync(int ruleId, int tenantId)
    {
        var rule = await context.WorkflowRules.FirstOrDefaultAsync(r => r.Id == ruleId && r.TenantId == tenantId) ?? throw new AppException(404, "Rule not found", "NOT_FOUND");

        context.WorkflowRules.Remove(rule);
        await context.SaveChangesAsync();

        return ruleId;
    }

    public async Task<int> CreateHolidayAsync(CreateHolidayRequest request, int tenantId)
    {
        if (await context.Holidays.AnyAsync(h => h.Date.Date == request.Date.Date && h.TenantId == tenantId))
        {
            throw new AppException(400, "A holiday already exists for this date", "DUPLICATE_HOLIDAY");
        }

        var holiday = new Holiday
        {
            TenantId = tenantId,
            Name = request.Name,
            Date = request.Date.Date
        };

        context.Holidays.Add(holiday);
        await context.SaveChangesAsync();

        return holiday.Id;
    }

    public async Task<List<HolidayResponse>> GetHolidaysAsync(int tenantId)
    {
        return await context.Holidays.Where(h => h.TenantId == tenantId).OrderBy(h => h.Date).Select(h => new HolidayResponse(h.Id, h.Name, h.Date)).ToListAsync();
    }

    public async Task DeleteHolidayRequestAsync(int holidayId, int tenantId)
    {
        var holiday = await context.Holidays.FirstOrDefaultAsync(h => h.Id == holidayId && h.TenantId == tenantId) ?? throw new AppException(404, "Holiday not found", "NOT_FOUND");

        context.Holidays.Remove(holiday);
        await context.SaveChangesAsync();
    }
}
