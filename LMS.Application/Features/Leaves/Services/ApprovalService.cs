using LMS.Application.Common.Interfaces;
using LMS.Application.Common.Modals;
using LMS.Application.Features.Leaves.DTOs.Manager;
using LMS.Application.Features.Leaves.Interfaces;
using LMS.Domain.Entities.Workflow;
using LMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Leaves.Services;

public class ApprovalService(IAppDbContext context) : IApprovalService
{
    public async Task<List<PendingApprovalResponse>> GetPendingApprovalsAsync(Guid userExternalId)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.ExternalId == userExternalId)
            ?? throw new AppException(404, "User not found", "NOT_FOUND");

        var userRoleId = user.RoleId;

        return await context.LeaveApprovalSteps
            .Include(a => a.LeaveRequest).ThenInclude(r => r.User)
            .Include(a => a.LeaveRequest).ThenInclude(r => r.LeaveType)
            .Where(a => a.Status == ApprovalStatus.Pending &&
                       (a.ApproverId == user.Id || a.RoleId == userRoleId))
            .OrderByDescending(a => a.LeaveRequest.CreatedAt)
            .Select(a => new PendingApprovalResponse(
                a.ExternalId,
                a.LeaveRequest.ExternalId,
                a.LeaveRequest.User.FirstName + " " + a.LeaveRequest.User.LastName,
                a.LeaveRequest.LeaveType.Name,
                a.LeaveRequest.StartDate,
                a.LeaveRequest.EndDate,
                a.LeaveRequest.TotalDays,
                a.LeaveRequest.Reason ?? "",
                a.LeaveRequest.CreatedAt
            ))
            .ToListAsync();
    }

    public async Task ProcessApprovalAsync(ProcessApprovalRequest request, Guid userExternalId)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.ExternalId == userExternalId)
            ?? throw new AppException(404, "User not found", "NOT_FOUND");

        var currentStep = await context.LeaveApprovalSteps
            .Include(a => a.LeaveRequest)
            .FirstOrDefaultAsync(a => a.ExternalId == request.ApprovalExternalId)
            ?? throw new AppException(404, "Approval step not found", "NOT_FOUND");

        bool isAuthorized = currentStep.ApproverId == user.Id || currentStep.RoleId == user.RoleId;

        if (!isAuthorized)
            throw new AppException(403, "You are not authorized to approve this step.", "FORBIDDEN");

        if (!request.IsApproved)
        {
            currentStep.Status = ApprovalStatus.Rejected;
            currentStep.ApproverId = user.Id;
            currentStep.ActionDate = DateTime.UtcNow;
            currentStep.Comments = request.Remarks;
            currentStep.LeaveRequest.Status = LeaveStatus.Rejected;
            currentStep.LeaveRequest.RejectionReason = request.Remarks;
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

                var balance = await context.LeaveBalances
                    .FirstOrDefaultAsync(b => b.UserId == currentStep.LeaveRequest.UserId
                                            && b.LeaveTypeId == currentStep.LeaveRequest.LeaveTypeId
                                            && b.Year == DateTime.UtcNow.Year);

                if (balance != null)
                {
                    if (balance.Balance < currentStep.LeaveRequest.TotalDays)
                        throw new AppException(400, "Insufficient leave balance at final approval stage.", "INSUFFICIENT_BALANCE");

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

    public async Task ForwardApprovalAsync(ForwardApprovalRequest request, Guid userExternalId)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.ExternalId == userExternalId)
            ?? throw new AppException(404, "User not found", "NOT_FOUND");

        var newApprover = await context.Users.FirstOrDefaultAsync(u => u.ExternalId == request.NewApproverExternalId)
            ?? throw new AppException(404, "Target approver not found", "USER_NOT_FOUND");

        var currentStep = await context.LeaveApprovalSteps
            .Include(a => a.LeaveRequest)
            .FirstOrDefaultAsync(a => a.ExternalId == request.ApprovalExternalId)
            ?? throw new AppException(404, "Step not found", "NOT_FOUND");

        if (currentStep.ApproverId != user.Id && currentStep.RoleId == null)
            throw new AppException(403, "Not authorized to forward this request", "FORBIDDEN");

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
    }
}
