using LMS.Application.Common.DTOs;
using LMS.Application.Common.Interfaces;
using LMS.Application.Common.Modals;
using LMS.Application.Features.Employees.DTOs;
using LMS.Application.Features.Employees.Interfaces;
using LMS.Domain.Entities.Auth;
using LMS.Domain.Enums;
using LMS.Domain.Enums.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Organization.Employees.Services;

public class EmployeeService(IAppDbContext context) : IEmployeeService
{
    public async Task<List<EmployeeLookupResponse>> GetEmployeeLookupsAsync(string query, int tenantId)
    {
        var userQuery = context.Users.Where(u => u.TenantId == tenantId && u.Status == UserStatus.Activated);

        if (!string.IsNullOrWhiteSpace(query))
        {
            query = query.ToLower().Trim();
            userQuery = userQuery.Where(u => u.FirstName.ToLower().Contains(query) || 
                                     u.LastName.ToLower().Contains(query) || 
                                     u.Email.ToLower().Contains(query));

        }
        return await userQuery.Select(u => new EmployeeLookupResponse(u.FirstName + " " + u.LastName, u.ExternalId, u.Role.Name)).Take(20).ToListAsync();
    }

    public async Task<PaginatedResult<EmployeeListResponse>> GetEmployeesAsync(QueryRequest request, Guid userExternalId, int tenantId)
    {
        var currentUser = await context.Users
            .Include(u => u.Role)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permissions)
            .FirstOrDefaultAsync(u => u.ExternalId == userExternalId && u.TenantId == tenantId) 
            ?? throw new AppException(404, "User context not found", "NOT_FOUND");

        var query = context.Users
            .Include(u => u.Department)
            .Include(u => u.Role)
            .Where(u => u.TenantId == currentUser.TenantId && u.Role.Code != "SUPER_ADMIN")
            .AsQueryable();

        var empMgmtPermission = currentUser.Role.RolePermissions.FirstOrDefault(rp => rp.Permissions.Name.StartsWith("EMPLOYEE_MGMT")) 
            ?? throw new AppException(403, "User does not have permission to get employees", "PERMISSION_DENIED");
        
        var scope = empMgmtPermission.Scope;

        if (scope == ScopeType.DEPARTMENT)
        {
            query = query.Where(u => u.DepartmentId == currentUser.DepartmentId);
        }
        else if (scope == ScopeType.SELF)
        {
            query = query.Where(u => u.Id == currentUser.Id);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower().Trim();
            query = query.Where(u => u.FirstName.ToLower().Contains(search) || 
                                     u.LastName.ToLower().Contains(search) || 
                                     u.Email.ToLower().Contains(search));
        }

        if (request.Filters != null)
        {
            if (request.Filters.TryGetValue("departmentId", out var deptIdStr) && Guid.TryParse(deptIdStr, out var deptExternalId))
            {
                query = query.Where(u => u.Department != null && u.Department.ExternalId == deptExternalId);
            }

            if (request.Filters.TryGetValue("status", out var statusStr))
            {
                if (int.TryParse(statusStr, out var statusInt) && Enum.IsDefined(typeof(UserStatus), statusInt))
                {
                    query = query.Where(u => (int)u.Status == statusInt);
                }
                else if (Enum.TryParse<UserStatus>(statusStr, true, out var statusEnum))
                {
                    query = query.Where(u => u.Status == statusEnum);
                }
            }
        }

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(u => u.CreatedAt)
                               .Skip((request.Page - 1) * request.PageSize)
                               .Take(request.PageSize)
                               .Select(u => new EmployeeListResponse(
                                    u.ExternalId,
                                    u.FirstName,
                                    u.LastName,
                                    u.Email,
                                    u.Department != null ? u.Department.Name : "N/A",
                                    u.Role.Name,
                                    u.Status,
                                    u.CreatedAt
                               )).ToListAsync();

        return new PaginatedResult<EmployeeListResponse>(items, totalCount);
    }

    public async Task<List<EmployeeListResponse>> GetRecentInvitesAsync(int tenantId)
    {
        return await context.Users
            .Include(u => u.Department)
            .Include(u => u.Role)
            .Where(u => u.TenantId == tenantId && 
                        u.Role.Code != "SUPER_ADMIN" && 
                        u.Status == UserStatus.Pending)
            .OrderByDescending(u => u.CreatedAt)
            .Take(2)
            .Select(u => new EmployeeListResponse(
                u.ExternalId,
                u.FirstName,
                u.LastName,
                u.Email,
                u.Department != null ? u.Department.Name : "N/A",
                u.Role.Name,
                u.Status,
                u.CreatedAt
            )).ToListAsync();
    }
}
