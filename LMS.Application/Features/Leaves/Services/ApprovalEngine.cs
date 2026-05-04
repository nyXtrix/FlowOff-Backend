using LMS.Application.Common.Interfaces;
using LMS.Application.Features.Leaves.Interfaces;
using LMS.Application.Features.Leaves.Models;
using LMS.Domain.Entities.Workflow;
using LMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LMS.Application.Common.Modals;

namespace LMS.Application.Features.Leaves.Services;

public class ApprovalEngine(IAppDbContext context, IRuleEvaluator evaluator, ILogger<ApprovalEngine> logger) : IApprovalEngine
{
    public async Task<List<LeaveApprovalStep>> GenerateApprovalChainAsync(Guid userExternalId, int leaveTypeId, decimal totalDays, int tenantId)
    {
        var chain = new List<LeaveApprovalStep>();
        var addedTargets = new HashSet<string>();

        var user = await context.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.ExternalId == userExternalId)
            ?? throw new AppException(404, "User not found.", "NOT_FOUND");

        var leaveType = await context.LeaveTypes.FindAsync(leaveTypeId);

        var evalContext = new EvaluationContext
        {
            Duration = totalDays,
            LeaveTypeCode = leaveType?.Code,
            UserRoleName = user.Role?.Name
        };

        if (user.ManagerId != null)
        {
            AddStepToChain(chain, addedTargets, approverId: user.ManagerId);
        }
        else
        {
            var superAdminRole = await context.Roles
                .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Code.ToUpper() == "SUPER_ADMIN");

            if (superAdminRole != null)
            {
                AddStepToChain(chain, addedTargets, roleId: superAdminRole.Id);
            }
            else
            {
                logger.LogCritical("Critical Error: Employee {UserId} has no manager and no SUPER_ADMIN role found for tenant {TenantId}.", user.Id, tenantId);
                throw new AppException(404, "Manager or Fallback Approver not found. Please contact system admin.", "NO_APPROVER_FOUND");
            }
        }

        var matchingRules = await context.ApprovalRules
            .Include(r => r.Steps)
            .Where(r => r.TenantId == tenantId && r.IsActive)
            .OrderBy(r => r.Priority)
            .ToListAsync();

        foreach (var rule in matchingRules)
        {
            if (evaluator.Evaluate(rule.ConditionJson, evalContext))
            {
                foreach (var s in rule.Steps.OrderBy(x => x.Sequence))
                {
                    if (s.ApproverType == ApproverType.Role && int.TryParse(s.ApproverValue, out var rid))
                    {
                        AddStepToChain(chain, addedTargets, roleId: rid);
                    }
                    else if (s.ApproverType == ApproverType.SpecificUser && int.TryParse(s.ApproverValue, out var uid))
                    {
                        AddStepToChain(chain, addedTargets, approverId: uid);
                    }
                }
            }
        }

        for (int i = 0; i < chain.Count; i++)
        {
            chain[i].StepOrder = i + 1;
            chain[i].Status = (i == 0) ? ApprovalStatus.Pending : ApprovalStatus.Waiting;
        }

        return chain;
    }

    private void AddStepToChain(List<LeaveApprovalStep> chain, HashSet<string> dedupe, int? approverId = null, int? roleId = null)
    {
        var key = approverId.HasValue ? $"USER:{approverId}" : $"ROLE:{roleId}";
        if (dedupe.Contains(key)) return;

        chain.Add(new LeaveApprovalStep
        {
            ApproverId = approverId,
            RoleId = roleId,
            Mode = roleId.HasValue ? ApprovalMode.AnyOne : ApprovalMode.Sequential
        });

        dedupe.Add(key);
    }
}
