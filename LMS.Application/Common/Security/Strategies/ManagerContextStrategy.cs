using LMS.Application.Common.Interfaces;
using LMS.Domain.Enums;
using LMS.Domain.Enums.Authorization;
using LMS.Domain.Module.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Common.Security.Strategies;

public class ManagerContextStrategy(IAppDbContext context) : IPermissionStrategy
{
    public int Priority => 30;

    public async Task ExecuteAsync(PermissionContext contextData)
    {
        var user = contextData.User;
        var p = contextData.Permissions;

        var hasReportees = await context.Users.AnyAsync(u => u.ManagerId == user.Id && u.Status == UserStatus.Activated);
        var isAssignedApprover = await context.LeaveApprovalSteps.AnyAsync(a => a.ApproverId == user.Id);
        var isRoleApprover = await context.LeaveApprovalSteps.AnyAsync(s => s.RoleId == user.RoleId && s.Status == ApprovalStatus.Pending);

        Console.WriteLine($"[PERM_RESOLVE] User: {user.FirstName} {user.LastName} (ID: {user.Id}, RoleId: {user.RoleId}) | HasReportees: {hasReportees} | IsAssigned: {isAssignedApprover} | IsRoleApprover: {isRoleApprover}");

        if (hasReportees)
        {
            EnsureModule(p, "TEAM", [ActionType.VIEW], ScopeType.TEAM);
        }

        if (hasReportees || isAssignedApprover || isRoleApprover)
        {
            EnsureModule(p, "APPROVALS", 
                [ActionType.VIEW, ActionType.CREATE, ActionType.UPDATE, ActionType.DELETE, ActionType.APPROVE, ActionType.REJECT], 
                ScopeType.TEAM);
        }
    }

    private void EnsureModule(AppPermissions p, string moduleId, ActionType[] actions, ScopeType scope)
    {
        if (!p.ContainsKey(moduleId))
        {
            p[moduleId] = new ModulePermission { Scope = scope, Actions = actions.ToList() };
        }
        else
        {
            foreach (var action in actions)
            {
                if (!p[moduleId].Actions.Contains(action))
                    p[moduleId].Actions.Add(action);
            }
        }
    }
}
