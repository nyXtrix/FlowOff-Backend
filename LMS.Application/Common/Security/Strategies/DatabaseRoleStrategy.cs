using LMS.Domain.Enums.Authorization;
using LMS.Domain.Module.Authorization;
using System.Text.Json;

namespace LMS.Application.Common.Security.Strategies;

public class DatabaseRoleStrategy : IPermissionStrategy
{
    public int Priority => 10;
    public Task ExecuteAsync(PermissionContext context)
    {
        var user = context.User;
        var role = user.Role;
        
        var rolePermissions = JsonSerializer.Deserialize<List<string>>(role.PermissionsJson ?? "[]") ?? new List<string>();
        var grantedPermissions = rolePermissions.ToHashSet();

        var overrides = JsonSerializer.Deserialize<List<PermissionOverrideDto>>(user.PermissionOverridesJson ?? "[]") ?? new List<PermissionOverrideDto>();
        foreach (var ov in overrides)
        {
            if (ov.IsAllowed) grantedPermissions.Add(ov.PermissionName);
            else grantedPermissions.Remove(ov.PermissionName);
        }

        foreach (var name in grantedPermissions)
        {
            var parts = name.Split('.');
            if (parts.Length != 2) continue;

            var moduleKey = parts[0];
            if (!Enum.TryParse<ActionType>(parts[1], out var actionType)) continue;

            var scope = role.Scope;

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

    private class PermissionOverrideDto
    {
        public string PermissionName { get; set; } = null!;
        public bool IsAllowed { get; set; }
    }
}
