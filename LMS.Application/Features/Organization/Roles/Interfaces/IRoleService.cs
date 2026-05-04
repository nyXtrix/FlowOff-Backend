using LMS.Application.Common.DTOs;
using LMS.Application.Features.Organization.Roles.DTOs;

namespace LMS.Application.Features.Organization.Roles.Interfaces;

public interface IRoleService
{
    Task<Guid> CreateRoleAsync(CreateRoleRequest request, int tenantId);
    Task UpdateRoleAsync(Guid id, UpdateRoleRequest request, int tenantId);
    Task DeleteRoleAsync(Guid id, int tenantId);
    Task<PaginatedResult<RoleResponse>> GetRolesAsync(int tenantId, QueryRequest request);
    Task RoleStatusAsync(Guid id, bool isActive, int tenantId);
}