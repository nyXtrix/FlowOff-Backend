using LMS.Application.Common.DTOs;
using LMS.Application.Common.Extension;
using LMS.Application.Common.Interfaces;
using LMS.Application.Common.Modals;
using LMS.Application.Features.Organization.Roles.DTOs;
using LMS.Application.Features.Organization.Roles.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Enums.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Organization.Roles.Services;

public class RoleService(IAppDbContext context, ICacheService cache) : IRoleService
{
    public async Task<Guid> CreateRoleAsync(CreateRoleRequest request, int tenantId)
    {
        if (await context.Roles.AnyAsync(r => (r.TenantId == tenantId || r.TenantId == null) && r.Name.ToLower() == request.Name.ToLower()))
        {
            throw new AppException(400, "A role with this name already exists.", "DUPLICATE_ROLE_NAME");
        }

        var role = new Role
        {
            Name = request.Name,
            Description = request.Description,
            Type = RoleType.CUSTOM,
            TenantId = tenantId,
            Code = $"CUSTOM_{request.Name.ToUpper().Replace(" ", "_")}_{tenantId}",
            IsActive = true
        };

        context.Roles.Add(role);
        await context.SaveChangesAsync();

        await MapPermissionsAsync(role.Id, request.Permissions);

        await cache.RemoveAsync($"lookup_role_{tenantId}");

        return role.ExternalId;
    }

    public async Task UpdateRoleAsync(Guid id, UpdateRoleRequest request, int tenantId)
    {
        var role = await context.Roles.Include(r => r.RolePermissions)
                                      .FirstOrDefaultAsync(r => r.ExternalId == id && r.TenantId == tenantId) ?? throw new AppException(404, "Role not found", "ROLE_NOT_FOUND");

        if (role.Type == RoleType.SYSTEM) throw new AppException(403, "System roles cannot be modified", "SYSTEM_ROLE_IMMUTABLE");

        if (request.Name != role.Name)
        {
            if (await context.Roles.AnyAsync(r => (r.TenantId == tenantId || r.TenantId == null) && r.Name.ToLower() == request.Name.ToLower()))
            {
                throw new AppException(400, "A role with this name already exists.", "DUPLICATE_ROLE_NAME");
            }
            role.Name = request.Name;
        }

        role.Description = request.Description;
        role.IsActive = request.IsActive;

        context.RolePermissions.RemoveRange(role.RolePermissions);

        await MapPermissionsAsync(role.Id, request.Permissions);

        await context.SaveChangesAsync();
        await cache.RemoveAsync($"lookup_role_{tenantId}");
        await cache.RemoveByPrefixAsync("upr_");
    }

    public async Task DeleteRoleAsync(Guid id, int tenantId)
    {
        var role = await context.Roles.FirstOrDefaultAsync(r => r.ExternalId == id && r.TenantId == tenantId)
                          ?? throw new AppException(404, "Role not found", "ROLE_NOT_FOUND");

        if (role.Type == RoleType.SYSTEM) throw new AppException(403, "System roles cannot be deleted", "SYSTEM_ROLE_IMMUTABLE");

        if (await context.UserRoles.AnyAsync(ur => ur.RoleId == role.Id))
        {
            throw new AppException(409, "Cannot delete role assigned to users", "ROLE_HAS_USERS");
        }

        context.Roles.Remove(role);
        await context.SaveChangesAsync();
        await cache.RemoveAsync($"lookup_role_{tenantId}");
        await cache.RemoveByPrefixAsync("upr_");
    }

    public async Task<PaginatedResult<RoleResponse>> GetRolesAsync(int tenantId, QueryRequest request)
    {
        var query = context.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permissions)
            .Where(r => r.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(r => r.Name.ToLower().Contains(search) || r.Description.ToLower().Contains(search));
        }

        if (request.Filters?.TryGetValue("isActive", out var activeStr) == true && bool.TryParse(activeStr, out var isActive))
        {
            query = query.Where(r => r.IsActive == isActive);
        }

        var result = await query
                     .OrderByDescending(r => r.Type)
                     .ToPaginatedResultAsync(request);

        var mappedItems = result.Items.Select(r => new RoleResponse
        (
            r.ExternalId,
            r.Name,
            r.Description,
            r.Type,
            r.IsActive,
            r.UserRoles?.Count() ?? 0,
            r.RolePermissions
                .GroupBy(rp => rp.Permissions.Name.Split('.', StringSplitOptions.None)[0])
                .ToDictionary(
                    g => g.Key,
                    g => new PermissionModuleDto(
                        g.Select(rp => rp.Permissions.Name.Split('.', StringSplitOptions.None)[1]).ToList(),
                        g.First().Scope
                    )
                )
        )).ToList();

        return new PaginatedResult<RoleResponse>(mappedItems, result.TotalCount);
    }


    private async Task MapPermissionsAsync(int roleId, Dictionary<string, PermissionModuleDto> permissions)
    {
        var rolePermissions = new List<RolePermission>();

        foreach (var module in permissions)
        {
            foreach (var action in module.Value.Actions)
            {
                var permissionName = $"{module.Key}.{action}";
                var permission = await context.Permissions.FirstOrDefaultAsync(p => p.Name == permissionName);

                if (permission != null)
                {
                    rolePermissions.Add(new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = permission.Id,
                        Scope = module.Value.Scope
                    });
                }
            }
        }

        if (rolePermissions.Count > 0)
        {
            await context.RolePermissions.AddRangeAsync(rolePermissions);
            await context.SaveChangesAsync();
        }
    }

    public async Task RoleStatusAsync(Guid id, bool isActive, int tenantId)
    {
        var role = await context.Roles.FirstOrDefaultAsync(r => r.ExternalId == id && r.TenantId == tenantId)
                         ?? throw new AppException(404, "Role not found", "ROLE_NOT_FOUND");

        if (role.Type == RoleType.SYSTEM)
        {
            throw new AppException(403, "System roles cannot be modified", "SYSTEM_ROLE_IMMUTABLE");
        }

        if (!isActive && await context.Users.AnyAsync(u => u.RoleId == role.Id))
        {
            throw new AppException(400, "Cannot deactivate a role that is currently assigned to users.", "ROLE_HAS_ACTIVE_USERS");
        }

        role.IsActive = isActive;
        await context.SaveChangesAsync();
        await cache.RemoveAsync($"lookup_role_{tenantId}");
        await cache.RemoveByPrefixAsync("upr_");
    }
}
