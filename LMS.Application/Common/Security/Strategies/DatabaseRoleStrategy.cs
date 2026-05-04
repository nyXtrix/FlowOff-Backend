using LMS.Domain.Enums.Authorization;
using LMS.Domain.Module.Authorization;

namespace LMS.Application.Common.Security.Strategies;

public class DatabaseRoleStrategy : IPermissionStrategy
{
    public int Priority => 10;
    public Task ExecuteAsync(PermissionContext context)
    {
        var user = context.User;
        var grantedPermissions = user.Role.RolePermissions.Select(rp => rp.Permissions.Name).ToHashSet();
        
        foreach (var ov in user.UserPermissionOverrides)
        {
            if (ov.IsAllowed) grantedPermissions.Add(ov.Permissions.Name);
            else grantedPermissions.Remove(ov.Permissions.Name);
        }

        foreach (var name in grantedPermissions)
        {
            var parts = name.Split('.');
            if (parts.Length != 2) continue;

            var moduleKey = parts[0];
            if (!Enum.TryParse<ActionType>(parts[1], out var actionType)) continue;

            if (!context.Permissions.ContainsKey(moduleKey))
                context.Permissions[moduleKey] = new ModulePermission { Scope = user.Role.Scope };

            if (!context.Permissions[moduleKey].Actions.Contains(actionType))
                context.Permissions[moduleKey].Actions.Add(actionType);
        }

        return Task.CompletedTask;
    }
}
