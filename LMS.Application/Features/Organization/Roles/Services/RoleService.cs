using LMS.Application.Common.DTOs;
using LMS.Application.Common.Extension;
using LMS.Application.Common.Interfaces;
using LMS.Application.Common.Modals;
using LMS.Application.Features.Organization.Roles.DTOs;
using LMS.Application.Features.Organization.Roles.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Enums.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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
            IsActive = true,
            PermissionsJson = SerializePermissions(request.Permissions)
        };

        context.Roles.Add(role);
        await context.SaveChangesAsync();

        await cache.RemoveAsync($"lookup_role_{tenantId}");

        return role.ExternalId;
    }

    public async Task UpdateRoleAsync(Guid id, UpdateRoleRequest request, int tenantId)
    {
        var role = await context.Roles
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
        role.PermissionsJson = SerializePermissions(request.Permissions);

        await context.SaveChangesAsync();
        await cache.RemoveAsync($"lookup_role_{tenantId}");
        await cache.RemoveByPrefixAsync("upr_");
    }

    public async Task DeleteRoleAsync(Guid id, int tenantId)
    {
        var role = await context.Roles.FirstOrDefaultAsync(r => r.ExternalId == id && r.TenantId == tenantId)
                          ?? throw new AppException(404, "Role not found", "ROLE_NOT_FOUND");

        if (role.Type == RoleType.SYSTEM) throw new AppException(403, "System roles cannot be deleted", "SYSTEM_ROLE_IMMUTABLE");

        if (await context.Users.AnyAsync(u => u.RoleId == role.Id))
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
            0,
            DeserializePermissions(r.PermissionsJson)
        )).ToList();

        return new PaginatedResult<RoleResponse>(mappedItems, result.TotalCount);
    }

    private string SerializePermissions(Dictionary<string, PermissionModuleDto> permissions)
    {
        var flatList = new List<string>();
        foreach (var module in permissions)
        {
            foreach (var action in module.Value.Actions)
            {
                flatList.Add($"{module.Key}.{action}");
            }
        }
        return JsonSerializer.Serialize(flatList);
    }

    private Dictionary<string, PermissionModuleDto> DeserializePermissions(string? json)
    {
        var flatList = JsonSerializer.Deserialize<List<string>>(json ?? "[]") ?? new List<string>();
        var modules = new Dictionary<string, PermissionModuleDto>();

        foreach (var p in flatList)
        {
            var parts = p.Split('.');
            if (parts.Length != 2) continue;

            if (!modules.ContainsKey(parts[0]))
            {
                modules[parts[0]] = new PermissionModuleDto(new List<string> { parts[1] }, ScopeType.SELF);
            }
            else
            {
                modules[parts[0]].Actions.Add(parts[1]);
            }
        }
        return modules;
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
