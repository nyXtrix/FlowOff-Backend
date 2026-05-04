using LMS.Application.Common.Interfaces;
using LMS.Application.Common.Modals;
using LMS.Application.Features.Leaves.DTOs.Employee;
using LMS.Application.Features.Leaves.DTOs.Admin;
using LMS.Application.Features.Leaves.Interfaces;
using LMS.Domain.Entities.Workflow;
using LMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using LMS.Application.Common.Extension;

namespace LMS.Application.Features.Leaves.Services;

public class LeaveService(
    IAppDbContext context, 
    IPolicyResolver policyResolver, 
    ILeaveCalculationEngine leaveCalculationEngine, 
    IApprovalEngine approvalEngine) : ILeaveService
{
    public async Task<Guid> ApplyLeaveAsync(ApplyLeaveRequest request, Guid userExternalId, int tenantId)
    {
        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var user = await context.Users.GetUserByExternalIdAsync(userExternalId);
            
            var leaveType = await context.LeaveTypes.FirstOrDefaultAsync(lt => lt.ExternalId == request.LeaveTypeExternalId && lt.TenantId == tenantId)
                ?? throw new AppException(404, "Leave type not found", "NOT_FOUND");

            var usagePolicy = await policyResolver.ResolveUsagePolicyAsync(userExternalId, tenantId);
            var weekOffPolicy = await policyResolver.ResolveWeekOffPolicyAsync(userExternalId, tenantId);

            var workingDays = await leaveCalculationEngine.CalculateLeaveDaysAsync(
                request.StartDate, 
                request.EndDate, 
                userExternalId, 
                tenantId, 
                usagePolicy, 
                weekOffPolicy);

            var balance = await context.LeaveBalances.FirstOrDefaultAsync(b => b.UserId == user.Id && b.LeaveTypeId == leaveType.Id && b.Year == DateTime.UtcNow.Year);
            if (balance == null || balance.Balance < workingDays)
            {
                throw new AppException(400, "Insufficient leave balance", "INSUFFICIENT_BALANCE");
            }

            var chainSteps = await approvalEngine.GenerateApprovalChainAsync(userExternalId, leaveType.Id, workingDays, tenantId);

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

            context.LeaveRequests.Add(leaveRequest);
            await context.SaveChangesAsync();

            foreach (var step in chainSteps)
            {
                step.LeaveRequestId = leaveRequest.Id;
                context.LeaveApprovalSteps.Add(step);
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return leaveRequest.ExternalId;
        }
        catch (Exception ex)
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
            .Include(r => r.ApprovalSteps)
            .FirstOrDefaultAsync(r => r.ExternalId == requestExternalId && r.UserId == user.Id)
            ?? throw new AppException(404, "Leave request not found", "NOT_FOUND");

        if (request.Status == LeaveStatus.Cancelled) return;

        var limit = request.LeaveType.MaxCancelableStep;
        if (request.ApprovalSteps.Any(a => a.StepOrder > limit && a.Status == ApprovalStatus.Approved))
        {
            throw new AppException(400, "Too late to cancel: request has already passed final cancellation stages.", "CANCEL_BLOCKED");
        }

        if (request.Status == LeaveStatus.Approved)
        {
            var balance = await context.LeaveBalances.FirstOrDefaultAsync(b => b.UserId == user.Id && b.LeaveTypeId == request.LeaveTypeId && b.Year == DateTime.UtcNow.Year);
            if (balance != null) balance.Balance += request.TotalDays;
        }

        request.Status = LeaveStatus.Cancelled;
        foreach (var app in request.ApprovalSteps.Where(a => a.Status == ApprovalStatus.Pending || a.Status == ApprovalStatus.Waiting))
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

}
