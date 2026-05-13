using LMS.Application.Common.DTOs;
using LMS.Application.Common.Interfaces;
using LMS.Application.Common.Modals;
using LMS.Application.Features.Employees.DTOs;
using LMS.Application.Features.Employees.Interfaces;
using LMS.Application.Common.Security;
using LMS.Domain.Entities.Auth;
using LMS.Domain.Enums;
using LMS.Domain.Enums.Authorization;
using LMS.Domain.Module.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Organization.Employees.Services;

public class EmployeeService(IAppDbContext context, IPermissionResolver permissionResolver) : IEmployeeService
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
                        u.Role.Code != "SUPER_ADMIN")
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

    public async Task<EmployeeProfileResponse> GetEmployeeProfileAsync(Guid employeeExternalId, Guid userExternalId, int tenantId)
    {
        var currentUser = await context.Users
            .Include(u => u.Role)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permissions)
            .Include(u => u.UserPermissionOverrides)
                .ThenInclude(ov => ov.Permissions)
            .FirstOrDefaultAsync(u => u.ExternalId == userExternalId && u.TenantId == tenantId)
            ?? throw new AppException(404, "User context not found", "NOT_FOUND");

        var user = await context.Users
            .Include(u => u.Role)
            .Include(u => u.Department)
            .Include(u => u.Manager)
            .FirstOrDefaultAsync(u => u.ExternalId == employeeExternalId && u.TenantId == tenantId)
            ?? throw new AppException(404, "Employee not found", "NOT_FOUND");

        bool isAuthorized = false;

        if (currentUser.Id == user.Id)
        {
            isAuthorized = true;
        }
        else
        {
            var permissions = await permissionResolver.ResolveForUserAsync(currentUser);

            if (permissions.TryGetValue("EMPLOYEE_MGMT", out var empMgmt))
            {
                if (empMgmt.Actions.Contains(ActionType.VIEW))
                {
                    if (empMgmt.Scope == ScopeType.ALL) isAuthorized = true;
                    else if (empMgmt.Scope == ScopeType.DEPARTMENT && user.DepartmentId == currentUser.DepartmentId) isAuthorized = true;
                    else if (empMgmt.Scope == ScopeType.TEAM && user.ManagerId == currentUser.Id) isAuthorized = true;
                }
            }

            if (!isAuthorized && permissions.TryGetValue("TEAM", out var teamPerm))
            {
                if (teamPerm.Actions.Contains(ActionType.VIEW))
                {
                    if (teamPerm.Scope == ScopeType.TEAM && user.ManagerId == currentUser.Id) isAuthorized = true;
                    else if (teamPerm.Scope == ScopeType.DEPARTMENT && user.DepartmentId == currentUser.DepartmentId) isAuthorized = true;
                }
            }
        }

        if (!isAuthorized)
        {
            throw new AppException(403, "You do not have permission to view this employee's profile", "FORBIDDEN");
        }

        var employeeDto = new EmployeeProfileDto
        {
            Id = user.ExternalId.ToString(),
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Gender = (int)user.Gender,
            Status = (int)user.Status,
            Role = user.Role.Name,
            RoleId = user.Role.ExternalId.ToString(),
            RoleCode = user.Role.Code,
            Department = user.Department?.Name,
            DepartmentId = user.Department?.ExternalId.ToString(),
            ManagerName = user.Manager != null ? $"{user.Manager.FirstName} {user.Manager.LastName}" : null,
            ManagerId = user.Manager?.ExternalId.ToString()
        };

        var currentYear = DateTime.UtcNow.Year;
        var balances = await context.LeaveBalances
            .Include(b => b.LeaveType)
            .Where(b => b.UserId == user.Id && b.Year == currentYear)
            .ToListAsync();

        var leaveRequests = await context.LeaveRequests
            .Include(r => r.LeaveType)
            .Where(r => r.UserId == user.Id && r.StartDate.Year == currentYear)
            .ToListAsync();

        var balanceDetails = balances.Select(b => {
            var pendingDays = (int)leaveRequests
                .Where(r => r.LeaveTypeId == b.LeaveTypeId && (r.Status == LeaveStatus.Pending || r.Status == LeaveStatus.InProgress))
                .Sum(r => (r.EndDate - r.StartDate).TotalDays + 1);

            return new LeaveBalanceDetailDto
            {
                Id = $"bal-{b.ExternalId}",
                Label = b.LeaveType.Name,
                Total = b.LeaveType.DefaultAnnualAllowence,
                Used = b.LeaveType.DefaultAnnualAllowence - b.Balance,
                Available = b.Balance,
                Pending = pendingDays
            };
        }).ToList();

        var statusCounts = new RequestStatusCountsDto
        {
            Total = leaveRequests.Count,
            Pending = leaveRequests.Count(r => r.Status == LeaveStatus.Pending || r.Status == LeaveStatus.InProgress),
            Approved = leaveRequests.Count(r => r.Status == LeaveStatus.Approved),
            Rejected = leaveRequests.Count(r => r.Status == LeaveStatus.Rejected),
            Cancelled = leaveRequests.Count(r => r.Status == LeaveStatus.Cancelled)
        };

        var leaveHistory = new List<Dictionary<string, object>>();
        var months = Enumerable.Range(1, DateTime.UtcNow.Month).Select(m => new DateTime(currentYear, m, 1));
        
        foreach (var month in months)
        {
            var monthData = new Dictionary<string, object>
            {
                { "month", month.ToString("MMMM") }
            };

            var monthRequests = leaveRequests
                .Where(r => r.Status == LeaveStatus.Approved && r.StartDate.Month == month.Month)
                .GroupBy(r => r.LeaveType.Name)
                .Select(g => new { Name = g.Key, Days = (int)g.Sum(r => (r.EndDate - r.StartDate).TotalDays + 1) });

            foreach (var item in monthRequests)
            {
                monthData[item.Name] = item.Days;
            }

            leaveHistory.Add(monthData);
        }

        return new EmployeeProfileResponse
        {
            Employee = employeeDto,
            LeaveBalances = balanceDetails,
            RequestStatusCounts = statusCounts,
            LeaveHistory = leaveHistory
        };
    }

    public async Task UpdateEmployeeProfileAsync(Guid employeeExternalId, UpdateEmployeeRequest request, Guid userExternalId, int tenantId)
    {
        var currentUser = await context.Users
            .Include(u => u.Role)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permissions)
            .FirstOrDefaultAsync(u => u.ExternalId == userExternalId && u.TenantId == tenantId)
            ?? throw new AppException(404, "User context not found", "NOT_FOUND");

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.ExternalId == employeeExternalId && u.TenantId == tenantId)
            ?? throw new AppException(404, "Employee not found", "NOT_FOUND");

        var permissions = await permissionResolver.ResolveForUserAsync(currentUser);
        bool canUpdate = false;
        bool isFullAdmin = false;

        if (permissions.TryGetValue("EMPLOYEE_MGMT", out var empMgmt))
        {
            if (empMgmt.Actions.Contains(ActionType.UPDATE))
            {
                if (empMgmt.Scope == ScopeType.ALL) { canUpdate = true; isFullAdmin = true; }
                else if (empMgmt.Scope == ScopeType.DEPARTMENT && user.DepartmentId == currentUser.DepartmentId) { canUpdate = true; isFullAdmin = true; }
                else if (empMgmt.Scope == ScopeType.TEAM && user.ManagerId == currentUser.Id) { canUpdate = true; isFullAdmin = true; }
                else if (empMgmt.Scope == ScopeType.SELF && user.Id == currentUser.Id) { canUpdate = true; }
            }
        }

        if (!canUpdate)
        {
            throw new AppException(403, "You do not have permission to update this employee's profile", "FORBIDDEN");
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        if (Enum.IsDefined(typeof(GenderEnum), request.Gender))
        {
            user.Gender = (GenderEnum)request.Gender;
        }

        if (isFullAdmin)
        {
            if (!string.IsNullOrWhiteSpace(request.DepartmentId) && Guid.TryParse(request.DepartmentId, out var deptExtId))
            {
                var dept = await context.Departments.FirstOrDefaultAsync(d => d.ExternalId == deptExtId && d.TenantId == tenantId);
                user.DepartmentId = dept?.Id;
            }
            else if (string.IsNullOrWhiteSpace(request.DepartmentId))
            {
                user.DepartmentId = null;
            }

            if (!string.IsNullOrWhiteSpace(request.ManagerId) && Guid.TryParse(request.ManagerId, out var managerExtId))
            {
                var manager = await context.Users.FirstOrDefaultAsync(u => u.ExternalId == managerExtId && u.TenantId == tenantId);
                user.ManagerId = manager?.Id;
            }
            else if (string.IsNullOrWhiteSpace(request.ManagerId))
            {
                user.ManagerId = null;
            }

            if (!string.IsNullOrWhiteSpace(request.RoleId) && Guid.TryParse(request.RoleId, out var roleExtId))
            {
                var role = await context.Roles.FirstOrDefaultAsync(r => r.ExternalId == roleExtId && (r.TenantId == tenantId || r.TenantId == null));
                if (role != null)
                {
                    user.RoleId = role.Id;
                }
            }
        }

        user.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }
}
