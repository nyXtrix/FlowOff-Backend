using LMS.Domain.Enums.Authorization;
using LMS.Domain.Module.Authorization;

namespace LMS.Application.Common.Security.Strategies;

public class SystemDefaultStrategy : IPermissionStrategy
{
    public int Priority => 20;

    public Task ExecuteAsync(PermissionContext context)
    {
        var p = context.Permissions;
        var roleCode = context.User.Role.Code;

        if (roleCode == "SUPER_ADMIN")
        {
            var allActions = Enum.GetValues<ActionType>().ToList();
            var modules = new[] { 
                "DASHBOARD", "POLICY", "LEAVE_MGMT", "EMPLOYEE_MGMT", 
                "ORGANIZATION", "APPROVALS", "ROLE_MGMT", "PROFILE", 
                "CALENDAR", "ADMIN_DASHBOARD", "TEAM",
                "NOTIFICATION"
            };

            foreach (var module in modules)
            {
                p[module] = new ModulePermission { 
                    Scope = ScopeType.ALL, 
                    Actions = allActions.ToList() 
                };
            }

            p.Remove("MY_LEAVES");
            return Task.CompletedTask;
        }

        else
        {
            EnsureModule(p, "PROFILE", [ActionType.VIEW], ScopeType.SELF);
            EnsureModule(p, "CALENDAR", [ActionType.VIEW], ScopeType.ALL);
            EnsureModule(p, "DASHBOARD", [ActionType.VIEW], ScopeType.SELF);

            p.Remove("MY_LEAVES");
            EnsureModule(p, "MY_LEAVES", [ActionType.VIEW, ActionType.CREATE, ActionType.UPDATE, ActionType.DELETE], ScopeType.SELF);
        }

        return Task.CompletedTask;
    }

    private static void EnsureModule(AppPermissions p, string moduleId, ActionType[] actions, ScopeType scope)
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
