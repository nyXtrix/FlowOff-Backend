using LMS.Application.Common.Interfaces;
using LMS.Application.Common.Modals;
using LMS.Application.Features.Leaves.DTOs.Admin;
using LMS.Application.Features.Leaves.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Entities.Leave;
using LMS.Domain.Entities.Workflow;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Leaves.Services;

public class LeaveConfigService(IAppDbContext context) : ILeaveConfigService
{
    public async Task<Guid> CreateLeaveTypeAsync(CreateLeaveTypeRequest request, int tenantId)
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
                    Year = DateTime.UtcNow.Year
                });
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return leaveType.ExternalId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Guid> CreateHolidayAsync(CreateHolidayRequest request, int tenantId)
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
        return holiday.ExternalId;
    }

    public async Task DeleteHolidayAsync(Guid holidayExternalId, int tenantId)
    {
        var holiday = await context.Holidays.FirstOrDefaultAsync(h => h.ExternalId == holidayExternalId && h.TenantId == tenantId) 
            ?? throw new AppException(404, "Holiday not found", "NOT_FOUND");

        context.Holidays.Remove(holiday);
        await context.SaveChangesAsync();
    }

    public async Task<Guid> CreateWorkflowRuleAsync(CreateWorkflowRuleRequest request, int tenantId)
    {
        int? leaveTypeId = null;
        if (request.LeaveTypeExternalId.HasValue)
        {
            var leaveType = await context.LeaveTypes.FirstOrDefaultAsync(lt => lt.ExternalId == request.LeaveTypeExternalId.Value && lt.TenantId == tenantId)
                ?? throw new AppException(404, "Leave type not found", "NOT_FOUND");
            leaveTypeId = leaveType.Id;
        }

        var rule = new WorkflowRule
        {
            TenantId = tenantId,
            LeaveTypeId = leaveTypeId,
            MinDays = request.MinDays,
            MaxDays = request.MaxDays
        };

        foreach (var stepDto in request.Steps.OrderBy(s => s.Sequence))
        {
            int? approverId = null;
            if (stepDto.ApproverExternalId.HasValue)
            {
                var user = await context.Users.FirstOrDefaultAsync(u => u.ExternalId == stepDto.ApproverExternalId.Value)
                    ?? throw new AppException(404, $"Approver not found: {stepDto.ApproverExternalId}", "USER_NOT_FOUND");
                approverId = user.Id;
            }

            int? roleId = null;
            if (stepDto.RoleExternalId.HasValue)
            {
                var role = await context.Roles.FirstOrDefaultAsync(r => r.ExternalId == stepDto.RoleExternalId.Value && r.TenantId == tenantId)
                    ?? throw new AppException(404, $"Role not found: {stepDto.RoleExternalId}", "ROLE_NOT_FOUND");
                roleId = role.Id;
            }

            rule.Steps.Add(new WorkflowStep
            {
                Sequence = stepDto.Sequence,
                ApproverType = stepDto.ApproverType,
                ApproverId = approverId,
                RoleId = roleId
            });
        }

        context.WorkflowRules.Add(rule);
        await context.SaveChangesAsync();
        return rule.ExternalId;
    }

    public async Task<List<WorkflowRuleResponse>> GetWorkflowRulesAsync(int tenantId)
    {
        return await context.WorkflowRules
            .Include(r => r.Steps).ThenInclude(s => s.Approver)
            .Include(r => r.Steps).ThenInclude(s => s.Role)
            .Include(r => r.LeaveType)
            .Where(r => r.TenantId == tenantId)
            .OrderBy(r => r.MinDays)
            .Select(r => new WorkflowRuleResponse(
                r.ExternalId,
                r.LeaveType != null ? r.LeaveType.Name : "General (All)",
                r.MinDays,
                r.MaxDays,
                r.Steps.Select(s => new WorkflowStepResponse(
                    s.Sequence, 
                    s.ApproverType, 
                    s.Approver != null ? (s.Approver.FirstName + " " + s.Approver.LastName) : null, 
                    s.Role != null ? s.Role.Name : null)).ToList()
            ))
            .ToListAsync();
    }

    public async Task DeleteWorkflowRuleAsync(Guid ruleExternalId, int tenantId)
    {
        var rule = await context.WorkflowRules.FirstOrDefaultAsync(r => r.ExternalId == ruleExternalId && r.TenantId == tenantId) 
            ?? throw new AppException(404, "Rule not found", "NOT_FOUND");

        context.WorkflowRules.Remove(rule);
        await context.SaveChangesAsync();
    }
}
