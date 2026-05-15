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
                .Where(r => r.Code.ToUpper() == "SUPER_ADMIN" && (r.TenantId == tenantId || r.TenantId == null))
                .OrderByDescending(r => r.TenantId)
                .FirstOrDefaultAsync();

            if (superAdminRole != null)
            {
                AddStepToChain(chain, addedTargets, roleId: superAdminRole.Id);
            }
        }

        var matchingRules = await context.WorkflowRules
            .Include(r => r.Steps)
            .Where(r => r.TenantId == tenantId && r.IsActive)
            .Where(r => r.LeaveTypeId == null || r.LeaveTypeId == leaveTypeId)
            .OrderBy(r => r.Priority)
            .ToListAsync();

        foreach (var rule in matchingRules)
        {
            if (rule.MinDays > 0 && totalDays < rule.MinDays) continue;
            if (rule.MaxDays > 0 && totalDays > rule.MaxDays) continue;

            if (evaluator.Evaluate(rule.ConditionJson, evalContext))
            {
                foreach (var s in rule.Steps.OrderBy(x => x.Sequence))
                {
                    if (s.ApproverType == ApproverType.MANAGER)
                    {
                        if (user.ManagerId.HasValue)
                        {
                            AddStepToChain(chain, addedTargets, approverId: user.ManagerId);
                        }
                    }
                    else if (s.ApproverType == ApproverType.ROLE)
                    {
                        if (int.TryParse(s.ApproverValue, out var rid))
                        {
                            AddStepToChain(chain, addedTargets, roleId: rid);
                        }
                        else if (Guid.TryParse(s.ApproverValue, out var guid))
                        {
                            var role = await context.Roles.FirstOrDefaultAsync(r => r.ExternalId == guid && (r.TenantId == tenantId || r.TenantId == null));
                            if (role != null) AddStepToChain(chain, addedTargets, roleId: role.Id);
                        }
                        else if (s.RoleId.HasValue)
                        {
                            AddStepToChain(chain, addedTargets, roleId: s.RoleId.Value);
                        }
                    }
                    else if (s.ApproverType == ApproverType.SPECIFIC_USER)
                    {
                        if (int.TryParse(s.ApproverValue, out var uid))
                        {
                            AddStepToChain(chain, addedTargets, approverId: uid);
                        }
                        else if (Guid.TryParse(s.ApproverValue, out var guid))
                        {
                            var targetUser = await context.Users.FirstOrDefaultAsync(u => u.ExternalId == guid && u.TenantId == tenantId);
                            if (targetUser != null) AddStepToChain(chain, addedTargets, approverId: targetUser.Id);
                        }
                        else if (s.ApproverId.HasValue)
                        {
                            AddStepToChain(chain, addedTargets, approverId: s.ApproverId.Value);
                        }
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
