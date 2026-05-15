using System;
using LMS.Application.Common.Interfaces;
using LMS.Application.Common.Modals;
using LMS.Application.Features.Leaves.DTOs.Employee;
using LMS.Application.Features.Leaves.DTOs.Admin;
using LMS.Application.Features.Leaves.Interfaces;
using LMS.Domain.Entities.Workflow;
using LMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using LMS.Application.Common.Extension;
using LMS.Application.Common.DTOs;
using LMS.Application.Features.Notifications.Interfaces;
using LMS.Application.Features.Leaves.DTOs.Manager;

namespace LMS.Application.Features.Leaves.Services;

public class LeaveService(
    IAppDbContext context,
    IPolicyResolver policyResolver,
    ILeaveCalculationEngine leaveCalculationEngine,
    IApprovalEngine approvalEngine,
    IApprovalService approvalService,
    INotificationService notificationService,
    ICacheService cache) : ILeaveService
{
    public async Task<Guid> ApplyLeaveAsync(ApplyLeaveRequest request, Guid userExternalId, int tenantId)
    {
        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var user = await context.Users.GetUserByExternalIdAsync(userExternalId);

            var hasOverlap = await context.LeaveRequests.AnyAsync(r => r.UserId == user.Id && r.Status != LeaveStatus.Cancelled && r.Status != LeaveStatus.Rejected
                                                                      && request.StartDate <= r.EndDate && request.EndDate >= r.StartDate);

            if (hasOverlap)
            {
                throw new AppException(400, "You already have a pending or approved leave request during this period", "OVERLAPPING_LEAVE");
            }

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

            balance.Balance -= workingDays;

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

            var firstStep = chainSteps.OrderBy(s => s.StepOrder).FirstOrDefault();
            if (firstStep != null && !firstStep.ApproverId.HasValue && firstStep.RoleId.HasValue)
            {
                await approvalService.ResolveApproverForRoleStepAsync(firstStep, userExternalId, tenantId);
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            await cache.RemoveAsync($"bal_{userExternalId}");
            await cache.RemoveByPrefixAsync($"hist_{userExternalId}");

            if (firstStep?.Status == ApprovalStatus.Approved)
            {
                await approvalService.ProcessApprovalAsync(new ProcessApprovalRequest(firstStep.ExternalId, true, "System: Auto-approved at creation"), userExternalId);
                return leaveRequest.ExternalId;
            }

            firstStep = chainSteps.FirstOrDefault(s => s.Status == ApprovalStatus.Pending);
            if (firstStep != null)
            {
                var tenantDomain = await context.Tenants
                    .Where(t => t.Id == tenantId)
                    .Select(t => t.Domain)
                    .FirstOrDefaultAsync() ?? "";

                var baseUrl = string.IsNullOrEmpty(tenantDomain) ? "" : $"/{tenantDomain}";
                var notificationTitle = "New Leave Request";
                var notificationMessage = $"{user.FirstName} {user.LastName} has applied for {leaveType.Name} from {request.StartDate:MMM dd} to {request.EndDate:MMM dd} ({workingDays} days).";

                if (firstStep.ApproverId.HasValue)
                {
                    var approverExternalId = await context.Users
                        .Where(u => u.Id == firstStep.ApproverId.Value)
                        .Select(u => u.ExternalId)
                        .FirstOrDefaultAsync();

                    if (approverExternalId != Guid.Empty)
                    {
                        await notificationService.SendNotificationAsync(approverExternalId, notificationTitle, notificationMessage, "Info", tenantId, $"{baseUrl}/approvals");
                        await cache.RemoveAsync($"appr_stats_{approverExternalId}");
                        await cache.RemoveByPrefixAsync($"appr_{approverExternalId}");
                    }
                }
                else if (firstStep.RoleId.HasValue)
                {
                    var approverExternalIds = await context.Users
                        .Where(u => u.RoleId == firstStep.RoleId.Value && u.TenantId == tenantId && u.Status == UserStatus.Activated)
                        .Select(u => u.ExternalId)
                        .ToListAsync();

                    foreach (var approverExtId in approverExternalIds)
                    {
                        await notificationService.SendNotificationAsync(approverExtId, notificationTitle, notificationMessage, "Info", tenantId, $"{baseUrl}/approvals");
                        await cache.RemoveAsync($"appr_stats_{approverExtId}");
                        await cache.RemoveByPrefixAsync($"appr_{approverExtId}");
                    }
                }
            }

            return leaveRequest.ExternalId;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<PaginatedResult<MyLeaveRequestResponse>> GetMyHistoryAsync(Guid userExternalId, QueryRequest request)
    {
        var cacheKey = $"hist_{userExternalId}_{request.Page}_{request.PageSize}_{request.SearchTerm}_{string.Join("_", request.Filters?.Select(f => f.Key + f.Value) ?? new List<string>())}";
        var cached = await cache.GetAsync<PaginatedResult<MyLeaveRequestResponse>>(cacheKey);
        if (cached != null) return cached;

        var user = await context.Users.FirstOrDefaultAsync(u => u.ExternalId == userExternalId)
            ?? throw new AppException(404, "User not found", "NOT_FOUND");

        var query = context.LeaveRequests
            .Include(r => r.LeaveType)
            .Where(r => r.UserId == user.Id);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower().Trim();
            query = query.Where(r => r.Reason.ToLower().Contains(search) || r.LeaveType.Name.ToLower().Contains(search));
        }

        if (request.Filters != null)
        {
            if (request.Filters.TryGetValue("status", out var statusStr))
            {
                if (int.TryParse(statusStr, out var statusInt) && Enum.IsDefined(typeof(LeaveStatus), statusInt))
                {
                    query = query.Where(r => (int)r.Status == statusInt);
                }
                else if (Enum.TryParse<LeaveStatus>(statusStr, true, out var status))
                {
                    query = query.Where(r => r.Status == status);
                }
            }

            if (request.Filters.TryGetValue("type", out var typeStr) && Guid.TryParse(typeStr, out var typeId))
            {
                query = query.Where(r => r.LeaveType.ExternalId == typeId);
            }

            if (request.Filters.TryGetValue("from", out var fromStr) && DateTime.TryParse(fromStr, out var fromDate))
            {
                query = query.Where(r => r.StartDate >= DateTime.SpecifyKind(fromDate.Date, DateTimeKind.Utc));
            }

            if (request.Filters.TryGetValue("to", out var toStr) && DateTime.TryParse(toStr, out var toDate))
            {
                query = query.Where(r => r.EndDate <= DateTime.SpecifyKind(toDate.Date, DateTimeKind.Utc));
            }
        }

        var result = await query
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
            .ToPaginatedResultAsync(request);

        await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
        return result;
    }

    public async Task<List<LeaveBalanceResponse>> GetMyBalancesAsync(Guid userExternalId)
    {
        var cacheKey = $"bal_{userExternalId}";
        var cached = await cache.GetAsync<List<LeaveBalanceResponse>>(cacheKey);
        if (cached != null) return cached;

        var user = await context.Users.FirstOrDefaultAsync(u => u.ExternalId == userExternalId)
            ?? throw new AppException(404, "User not found", "NOT_FOUND");

        var result = await context.LeaveBalances
            .Include(b => b.LeaveType)
            .Where(b => b.UserId == user.Id && b.Year == DateTime.UtcNow.Year)
            .Select(b => new LeaveBalanceResponse(
                b.LeaveType.ExternalId,
                b.LeaveType.Name,
                b.Balance,
                b.LeaveType.DefaultAnnualAllowence,
                b.Year
            ))
            .ToListAsync();

        await cache.SetAsync(cacheKey, result, TimeSpan.FromHours(1));
        return result;
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

        if (request.Status == LeaveStatus.Approved || request.Status == LeaveStatus.Pending)
        {
            var balance = await context.LeaveBalances.FirstOrDefaultAsync(b => b.UserId == user.Id && b.LeaveTypeId == request.LeaveTypeId && b.Year == DateTime.UtcNow.Year);
            if (balance != null) balance.Balance += request.TotalDays;
        }

        request.Status = LeaveStatus.Cancelled;
        
        var affectedUserIds = request.ApprovalSteps
            .Where(a => a.ApproverId.HasValue)
            .Select(a => a.ApproverId!.Value)
            .Distinct()
            .ToList();

        var affectedRoleIds = request.ApprovalSteps
            .Where(a => a.RoleId.HasValue && (a.Status == ApprovalStatus.Pending || a.Status == ApprovalStatus.Waiting))
            .Select(a => a.RoleId!.Value)
            .Distinct()
            .ToList();

        if (affectedRoleIds.Any())
        {
            var roleUserIds = await context.Users
                .Where(u => affectedRoleIds.Contains(u.RoleId) && u.TenantId == user.TenantId)
                .Select(u => u.Id)
                .ToListAsync();
            affectedUserIds.AddRange(roleUserIds);
        }

        foreach (var app in request.ApprovalSteps.Where(a => a.Status == ApprovalStatus.Pending || a.Status == ApprovalStatus.Waiting))
        {
            app.Status = ApprovalStatus.Skipped;
        }

        await context.SaveChangesAsync();

        await cache.RemoveAsync($"bal_{userExternalId}");
        await cache.RemoveByPrefixAsync($"hist_{userExternalId}");

        foreach (var affectedId in affectedUserIds.Distinct())
        {
            var affectedExtId = await context.Users
                .Where(u => u.Id == affectedId)
                .Select(u => u.ExternalId)
                .FirstOrDefaultAsync();

            if (affectedExtId != Guid.Empty)
            {
                await cache.RemoveAsync($"appr_stats_{affectedExtId}");
                await cache.RemoveByPrefixAsync($"appr_{affectedExtId}");
            }
        }
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

    public async Task<decimal> CalculateActualDaysAsync(DateTime startDate, DateTime endDate, Guid userExternalId, int tenantId)
    {
        var utcStart = DateTime.SpecifyKind(startDate.Date, DateTimeKind.Utc);
        var utcEnd = DateTime.SpecifyKind(endDate.Date, DateTimeKind.Utc);

        try
        {
            var usagePolicy = await policyResolver.ResolveUsagePolicyAsync(userExternalId, tenantId);
            var weekOffPolicy = await policyResolver.ResolveWeekOffPolicyAsync(userExternalId, tenantId);

            return await leaveCalculationEngine.CalculateLeaveDaysAsync(
                utcStart,
                utcEnd,
                userExternalId,
                tenantId,
                usagePolicy,
                weekOffPolicy);
        }
        catch (AppException ex) when (ex.ErrorCode == "NOT_FOUND")
        {
            return (decimal)(utcEnd - utcStart).TotalDays + 1;
        }
    }
}
