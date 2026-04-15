using LMS.Application.Common.Interfaces;
using LMS.Application.Common.Modals;
using LMS.Application.Features.Leaves.DTOs.Employee;
using LMS.Application.Features.Leaves.DTOs.Admin;
using LMS.Application.Features.Leaves.Interfaces;
using LMS.Domain.Entities.Workflow;
using LMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Leaves.Services;

public class LeaveService(IAppDbContext context) : ILeaveService
{
    public async Task<Guid> ApplyLeaveAsync(ApplyLeaveRequest request, Guid userExternalId, int tenantId)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.ExternalId == userExternalId)
            ?? throw new AppException(404, "User not found", "NOT_FOUND");

        var leaveType = await context.LeaveTypes.FirstOrDefaultAsync(lt => lt.ExternalId == request.LeaveTypeExternalId && lt.TenantId == tenantId)
            ?? throw new AppException(404, "Leave type not found", "NOT_FOUND");

        var workingDays = await CalculateActualWorkingDaysAsync(tenantId, request.StartDate, request.EndDate);

        var balance = await context.LeaveBalances.FirstOrDefaultAsync(b => b.UserId == user.Id && b.LeaveTypeId == leaveType.Id && b.Year == DateTime.UtcNow.Year);
        if (balance == null || balance.Balance < workingDays)
        {
            throw new AppException(400, "Insufficient leave balance", "INSUFFICIENT_BALANCE");
        }

        var leaveRequest = new LeaveRequest
        {
            UserId = user.Id,
            TenantId = tenantId,
            LeaveTypeId = leaveType.Id,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Reason = request.Reason,
            Status = LeaveStatus.Pending,
            TotalDays = workingDays,
            CreatedAt = DateTime.UtcNow
        };

        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.LeaveRequests.Add(leaveRequest);
            await context.SaveChangesAsync();

            await InitializeApprovalChainAsync(leaveRequest);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return leaveRequest.ExternalId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<MyLeaveRequestResponse>> GetMyHistoryAsync(Guid userExternalId)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.ExternalId == userExternalId)
            ?? throw new AppException(404, "User not found", "NOT_FOUND");

        return await context.LeaveRequests
            .Include(r => r.LeaveType)
            .Where(r => r.UserId == user.Id)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new MyLeaveRequestResponse(
                r.ExternalId,
                r.LeaveType.Name,
                r.StartDate,
                r.EndDate,
                r.TotalDays,
                r.Status.ToString(),
                r.CreatedAt
            ))
            .ToListAsync();
    }

    public async Task<List<LeaveBalanceResponse>> GetMyBalancesAsync(Guid userExternalId)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.ExternalId == userExternalId)
            ?? throw new AppException(404, "User not found", "NOT_FOUND");

        return await context.LeaveBalances
            .Include(b => b.LeaveType)
            .Where(b => b.UserId == user.Id && b.Year == DateTime.UtcNow.Year)
            .Select(b => new LeaveBalanceResponse(
                b.LeaveType.ExternalId,
                b.LeaveType.Name,
                b.Balance,
                b.Year
            ))
            .ToListAsync();
    }

    public async Task CancelRequestAsync(Guid requestExternalId, Guid userExternalId)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.ExternalId == userExternalId)
            ?? throw new AppException(404, "User not found", "NOT_FOUND");

        var request = await context.LeaveRequests
            .Include(r => r.LeaveType)
            .Include(r => r.Approvals)
            .FirstOrDefaultAsync(r => r.ExternalId == requestExternalId && r.UserId == user.Id)
            ?? throw new AppException(404, "Leave request not found", "NOT_FOUND");

        if (request.Status == LeaveStatus.Cancelled) return;

        var limit = request.LeaveType.MaxCancelableStep;
        if (request.Approvals.Any(a => a.Sequence > limit && a.Status == ApprovalStatus.Approved))
        {
            throw new AppException(400, "Too late to cancel: request has already passed final cancellation stages.", "CANCEL_BLOCKED");
        }

        if (request.Status == LeaveStatus.Approved)
        {
            var balance = await context.LeaveBalances.FirstOrDefaultAsync(b => b.UserId == user.Id && b.LeaveTypeId == request.LeaveTypeId && b.Year == DateTime.UtcNow.Year);
            if (balance != null) balance.Balance += request.TotalDays;
        }

        request.Status = LeaveStatus.Cancelled;
        foreach (var app in request.Approvals.Where(a => a.Status == ApprovalStatus.Pending || a.Status == ApprovalStatus.Waiting))
        {
            app.Status = ApprovalStatus.Skipped;
        }

        await context.SaveChangesAsync();
    }

    public async Task<List<HolidayResponse>> GetHolidaysAsync(int tenantId)
    {
        return await context.Holidays
            .Where(h => h.TenantId == tenantId)
            .OrderBy(h => h.Date)
            .Select(h => new HolidayResponse(h.ExternalId, h.Name, h.Date))
            .ToListAsync();
    }

    public async Task<List<LeaveTypeResponse>> GetAvailableLeaveTypesAsync(int tenantId)
    {
        return await context.LeaveTypes
            .Where(lt => lt.TenantId == tenantId)
            .Select(lt => new LeaveTypeResponse(lt.ExternalId, lt.Name, lt.Description, lt.MaxCancelableStep, lt.DefaultAnnualAllowence))
            .ToListAsync();
    }

    private async Task<decimal> CalculateActualWorkingDaysAsync(int tenantId, DateTime start, DateTime end)
    {
        var tenant = await context.Tenants.FindAsync(tenantId);
        var weekOffs = tenant?.WeekoffDays?.Split(',').Select(int.Parse).ToList() ?? [0, 6]; // Default Sat/Sun

        var holidays = await context.Holidays.Where(h => h.TenantId == tenantId && h.Date >= start && h.Date <= end).Select(h => h.Date.Date).ToListAsync();

        decimal totalDays = 0;
        for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
        {
            if (weekOffs.Contains((int)date.DayOfWeek) || holidays.Contains(date)) continue;
            totalDays++;
        }
        return totalDays;
    }

    private async Task InitializeApprovalChainAsync(LeaveRequest request)
    {
        var rule = await context.WorkflowRules
            .Include(r => r.Steps)
            .Where(r => r.TenantId == request.TenantId
                     && (r.LeaveTypeId == null || r.LeaveTypeId == request.LeaveTypeId)
                     && request.TotalDays >= r.MinDays
                     && (r.MaxDays == 0 || request.TotalDays <= r.MaxDays))
            .OrderByDescending(r => r.LeaveTypeId)
            .FirstOrDefaultAsync();

        if (rule == null || !rule.Steps.Any())
        {
            throw new AppException(400, "No approval workflow found for this request. Please contact HR.", "NO_WORKFLOW_FOUND");
        }

        foreach (var step in rule.Steps.OrderBy(s => s.Sequence))
        {
            var approverId = await ResolveApproverId(step, request.UserId);
            context.LeaveApprovals.Add(new LeaveApproval
            {
                LeaveRequestId = request.Id,
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
            if (user == null || !user.ManagerId.HasValue)
            {
                throw new AppException(400, "Reporter's manager not found. Please update your profile.", "MANAGER_NOT_FOUND");
            }
            return user.ManagerId.Value;
        }
        
        return step.ApproverId ?? throw new AppException(400, "Workflow configuration error: Specific approver missing in rule.", "RULE_CONFIG_ERROR");
    }
}
