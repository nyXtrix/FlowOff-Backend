using LMS.Application.Common.DTOs;
using LMS.Application.Common.Interfaces;
using LMS.Application.Common.Modals;
using LMS.Application.Features.Leaves.DTOs.Manager;
using LMS.Application.Features.Leaves.Interfaces;
using LMS.Application.Features.Notifications.Interfaces;
using LMS.Domain.Entities.Workflow;
using LMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Leaves.Services;

public class ApprovalService(IAppDbContext context, INotificationService notificationService) : IApprovalService
{
    public async Task<PaginatedResult<ApprovalListResponse>> GetApprovalsAsync(QueryRequest request, Guid userExternalId)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.ExternalId == userExternalId)
            ?? throw new AppException(404, "User not found", "NOT_FOUND");

        var query = context.LeaveApprovalSteps.Include(a => a.LeaveRequest).ThenInclude(r => r.User)
                            .Include(r => r.LeaveRequest).ThenInclude(r => r.LeaveType)
                            .Where(a => (a.ApproverId == user.Id || a.RoleId == user.RoleId) && a.LeaveRequest.TenantId == user.TenantId)
                            .AsQueryable();

        if (request.Filters != null && request.Filters.TryGetValue("status", out var statusStr))
        {
            if (int.TryParse(statusStr, out var statusInt) && Enum.IsDefined(typeof(ApprovalStatus), statusInt))
            {
                query = query.Where(a => (int)a.Status == statusInt);
            }
        }
        else
        {
            query = query.Where(a => a.Status != ApprovalStatus.Waiting);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower().Trim();

            query = query.Where(a => a.LeaveRequest.User.FirstName.ToLower().Contains(search) || a.LeaveRequest.User.LastName.ToLower().Contains(search)
                                     || a.LeaveRequest.LeaveType.Name.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(a => a.LeaveRequest.CreatedAt).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
                               .Select(a => new ApprovalListResponse(
                                a.ExternalId,
                                a.LeaveRequest.ExternalId,
                                a.LeaveRequest.User.FirstName + " " + a.LeaveRequest.User.LastName,
                                a.LeaveRequest.LeaveType.Name,
                                a.LeaveRequest.StartDate,
                                a.LeaveRequest.EndDate,
                                a.LeaveRequest.TotalDays,
                                a.LeaveRequest.Reason ?? "",
                                a.LeaveRequest.CreatedAt,
                                a.Status
                               )).ToListAsync();

        return new PaginatedResult<ApprovalListResponse>(items, totalCount);
    }

    public async Task ProcessApprovalAsync(ProcessApprovalRequest request, Guid userExternalId)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.ExternalId == userExternalId)
            ?? throw new AppException(404, "User not found", "NOT_FOUND");

        var currentStep = await context.LeaveApprovalSteps
            .Include(a => a.LeaveRequest).ThenInclude(r => r.User)
            .Include(a => a.LeaveRequest).ThenInclude(r => r.LeaveType)
            .FirstOrDefaultAsync(a => a.ExternalId == request.ApprovalExternalId)
            ?? throw new AppException(404, "Approval step not found", "NOT_FOUND");

        bool isAuthorized = (currentStep.ApproverId == user.Id || currentStep.RoleId == user.RoleId) 
                            && currentStep.LeaveRequest.TenantId == user.TenantId;

        if (!isAuthorized)
            throw new AppException(403, "You are not authorized to approve this step.", "FORBIDDEN");

        var tenantId = currentStep.LeaveRequest.TenantId;
        var applicant = currentStep.LeaveRequest.User;

        var tenantDomain = await context.Tenants
            .Where(t => t.Id == tenantId)
            .Select(t => t.Domain)
            .FirstOrDefaultAsync() ?? "";
        var baseUrl = string.IsNullOrEmpty(tenantDomain) ? "" : $"/{tenantDomain}";

        if (!request.IsApproved)
        {
            currentStep.Status = ApprovalStatus.Rejected;
            currentStep.ApproverId = user.Id;
            currentStep.ActionDate = DateTime.UtcNow;
            currentStep.Comments = request.Remarks;
            currentStep.LeaveRequest.Status = LeaveStatus.Rejected;
            currentStep.LeaveRequest.RejectionReason = request.Remarks;

            var balance = await context.LeaveBalances.FirstOrDefaultAsync(b => b.UserId == currentStep.LeaveRequest.UserId && b.LeaveTypeId == currentStep.LeaveRequest.LeaveTypeId
                                                                                && b.Year == DateTime.UtcNow.Year);
            if (balance != null)
            {
                balance.Balance += currentStep.LeaveRequest.TotalDays;
            }

            await notificationService.SendNotificationAsync(applicant.ExternalId, "Leave Rejected", $"Your leave request for {currentStep.LeaveRequest.LeaveType.Name} has been rejected by {user.FirstName}.", "Error", tenantId, $"{baseUrl}/leaves");
        }
        else
        {
            currentStep.Status = ApprovalStatus.Approved;
            currentStep.ApproverId = user.Id;
            currentStep.ActionDate = DateTime.UtcNow;
            currentStep.Comments = request.Remarks;

            var nextStep = await context.LeaveApprovalSteps
                .Where(a => a.LeaveRequestId == currentStep.LeaveRequestId && a.StepOrder == currentStep.StepOrder + 1)
                .FirstOrDefaultAsync();

            if (nextStep == null)
            {
                currentStep.LeaveRequest.Status = LeaveStatus.Approved;
                await notificationService.SendNotificationAsync(applicant.ExternalId, "Leave Approved", $"Your leave request for {currentStep.LeaveRequest.LeaveType.Name} has been fully approved.", "Success", tenantId, $"{baseUrl}/leaves");
            }
            else
            {
                nextStep.Status = ApprovalStatus.Pending;
                var notificationTitle = "Leave Approval Required";
                var notificationMessage = $"{applicant.FirstName} {applicant.LastName} has a leave request awaiting your approval.";

                if (nextStep.ApproverId.HasValue)
                {
                    var approverExtId = await context.Users.Where(u => u.Id == nextStep.ApproverId.Value).Select(u => u.ExternalId).FirstOrDefaultAsync();
                    if (approverExtId != Guid.Empty)
                        await notificationService.SendNotificationAsync(approverExtId, notificationTitle, notificationMessage, "Info", tenantId, $"{baseUrl}/approvals");
                }
                else if (nextStep.RoleId.HasValue)
                {
                    var approverExtIds = await context.UserRoles.Where(ur => ur.RoleId == nextStep.RoleId.Value).Select(ur => ur.User.ExternalId).ToListAsync();
                    foreach (var extId in approverExtIds)
                        await notificationService.SendNotificationAsync(extId, notificationTitle, notificationMessage, "Info", tenantId, $"{baseUrl}/approvals");
                }
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task ForwardApprovalAsync(ForwardApprovalRequest request, Guid userExternalId)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.ExternalId == userExternalId)
            ?? throw new AppException(404, "User not found", "NOT_FOUND");

        var newApprover = await context.Users.FirstOrDefaultAsync(u => u.ExternalId == request.NewApproverExternalId)
            ?? throw new AppException(404, "Target approver not found", "USER_NOT_FOUND");

        var currentStep = await context.LeaveApprovalSteps
            .Include(a => a.LeaveRequest).ThenInclude(r => r.User)
            .FirstOrDefaultAsync(a => a.ExternalId == request.ApprovalExternalId)
            ?? throw new AppException(404, "Step not found", "NOT_FOUND");

        if ((currentStep.ApproverId != user.Id && currentStep.RoleId == null) || currentStep.LeaveRequest.TenantId != user.TenantId)
            throw new AppException(403, "Not authorized to forward this request", "FORBIDDEN");

        var tenantId = currentStep.LeaveRequest.TenantId;
        var applicant = currentStep.LeaveRequest.User;

        var tenantDomain = await context.Tenants
            .Where(t => t.Id == tenantId)
            .Select(t => t.Domain)
            .FirstOrDefaultAsync() ?? "";
        var baseUrl = string.IsNullOrEmpty(tenantDomain) ? "" : $"/{tenantDomain}";

        currentStep.Status = ApprovalStatus.Approved;
        currentStep.ApproverId = user.Id;
        currentStep.ActionDate = DateTime.UtcNow;
        currentStep.Comments = "Forwarded: " + request.Remarks;

        var subsequentSteps = await context.LeaveApprovalSteps
            .Where(a => a.LeaveRequestId == currentStep.LeaveRequestId && a.StepOrder > currentStep.StepOrder)
            .ToListAsync();

        foreach (var step in subsequentSteps)
        {
            step.StepOrder += 1;
        }

        context.LeaveApprovalSteps.Add(new LeaveApprovalStep
        {
            LeaveRequestId = currentStep.LeaveRequestId,
            ApproverId = newApprover.Id,
            StepOrder = currentStep.StepOrder + 1,
            Status = ApprovalStatus.Pending,
            Comments = "Manually forwarded"
        });

        await context.SaveChangesAsync();

        await notificationService.SendNotificationAsync(newApprover.ExternalId, "Leave Approval Forwarded", $"{applicant.FirstName}'s leave request has been forwarded to you for approval by {user.FirstName}.", "Info", tenantId, $"{baseUrl}/approvals");
    }

    public async Task<ApprovalStatsResponse> GetApprovalStatsAsync(Guid userExternalId)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.ExternalId == userExternalId)
            ?? throw new AppException(404, "User not found", "NOT_FOUND");

        var baseQuery = context.LeaveApprovalSteps
            .Include(a => a.LeaveRequest)
            .Where(a => (a.ApproverId == user.Id || a.RoleId == user.RoleId) && a.LeaveRequest.TenantId == user.TenantId);

        var pendingCount = await baseQuery.CountAsync(a => a.Status == ApprovalStatus.Pending);
        
        var today = DateTime.UtcNow.Date;
        var todayRequests = await baseQuery.CountAsync(a => a.Status == ApprovalStatus.Pending && a.LeaveRequest.CreatedAt >= today);

        var resolvedRequests = await baseQuery
            .Where(a => a.Status != ApprovalStatus.Waiting && a.ActionDate.HasValue)
            .Select(a => new { a.LeaveRequest.CreatedAt, ActionDate = a.ActionDate.Value })
            .ToListAsync();

        string responseTimeStr = "N/A";
        string responseTimeSubtitle = "No resolved requests";
        if (resolvedRequests.Any())
        {
            var avgHours = resolvedRequests.Average(a => (a.ActionDate - a.CreatedAt).TotalHours);
            if (avgHours < 1)
            {
                responseTimeStr = "< 1h";
                responseTimeSubtitle = "Excellent response time";
            }
            else if (avgHours < 24)
            {
                responseTimeStr = $"~{Math.Round(avgHours)}h";
                responseTimeSubtitle = "Average response time";
            }
            else
            {
                responseTimeStr = $"~{Math.Round(avgHours / 24)}d";
                responseTimeSubtitle = "Average response time";
            }
        }
        else if (pendingCount == 0)
        {
            responseTimeStr = "100%";
            responseTimeSubtitle = "All Caught Up";
        }

        var totalProcessed = await baseQuery.CountAsync(a => a.Status == ApprovalStatus.Approved || a.Status == ApprovalStatus.Rejected || a.Status == ApprovalStatus.Skipped);
        var approvedCount = await baseQuery.CountAsync(a => a.Status == ApprovalStatus.Approved);
        var approvalRate = totalProcessed > 0 ? (int)Math.Round((double)approvedCount / totalProcessed * 100) : 100;

        var stats = new List<StatCardDto>
        {
            new StatCardDto("PENDING_QUEUE", "Pending Queue", pendingCount.ToString(), "Requests awaiting action"),
            new StatCardDto("TODAY_REQUESTS", "Today's Requests", todayRequests.ToString(), "Received in last 24h"),
            new StatCardDto("RESPONSE_TIME", "Avg Response Time", responseTimeStr, responseTimeSubtitle),
            new StatCardDto("APPROVAL_RATE", "Approval Rate", $"{approvalRate}%", "Overall approval rate")
        };

        return new ApprovalStatsResponse(stats);
    }
}
