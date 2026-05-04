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

        EnsureModule(p, "PROFILE", [ActionType.VIEW], ScopeType.SELF);
        EnsureModule(p, "CALENDAR", [ActionType.VIEW], ScopeType.ALL);

        if (roleCode == "SUPER_ADMIN")
        {
            EnsureModule(p, "ADMIN_DASHBOARD", [ActionType.VIEW], ScopeType.ALL);
            p.Remove("DASHBOARD");
            p.Remove("MY_LEAVES");
        }
        else
        {
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
