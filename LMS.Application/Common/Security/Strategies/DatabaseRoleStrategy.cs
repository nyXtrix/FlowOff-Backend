using LMS.Domain.Enums.Authorization;
using LMS.Domain.Module.Authorization;

namespace LMS.Application.Common.Security.Strategies;

public class DatabaseRoleStrategy : IPermissionStrategy
{
    public int Priority => 10;
    public Task ExecuteAsync(PermissionContext context)
    {
        var user = context.User;
        
        var permissionMap = user.Role.RolePermissions
            .Select(rp => new { rp.Permissions.Name, rp.Scope })
            .ToList();

        var grantedPermissions = permissionMap.Select(p => p.Name).ToHashSet();
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

            var scope = permissionMap.FirstOrDefault(p => p.Name == name)?.Scope ?? user.Role.Scope;

            if (!context.Permissions.ContainsKey(moduleKey))
            {
                context.Permissions[moduleKey] = new ModulePermission { Scope = scope };
            }
            else
            {
                if (scope > context.Permissions[moduleKey].Scope)
                {
                    context.Permissions[moduleKey].Scope = scope;
                }
            }

            if (!context.Permissions[moduleKey].Actions.Contains(actionType))
                context.Permissions[moduleKey].Actions.Add(actionType);
        }

        return Task.CompletedTask;
    }
}
