using LMS.Application.Common.DTOs;
using LMS.Application.Common.Extension;
using LMS.Application.Common.Interfaces;
using LMS.Application.Common.Modals;
using LMS.Application.Features.Organization.Department.DTOs;
using LMS.Application.Features.Organization.Department.Interfaces;
using LMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Organization.Department.Services;

public class DepartmentService(IAppDbContext context) : IDepartmentService
{
    public async Task<Guid> CreateDepartmentAsync(CreateDepartmentRequest request, int tenantId)
    {
        if (await context.Departments.AnyAsync(d => d.TenantId == tenantId && d.Name.ToLower() == request.Name.ToLower()))
        {
            throw new AppException(400, "A department with this name already exists.", "DUPLICATE_DEPARTMENT_NAME");
        }

        var department = new Domain.Entities.Organization.Department
        {
            Name = request.Name,
            Description = request.Description,
            TenantId = tenantId,
            IsActive = request.IsActive
        };

        context.Departments.Add(department);
        await context.SaveChangesAsync(CancellationToken.None);

        return department.ExternalId;
    }

    public async Task UpdateDepartmentAsync(Guid id, UpdateDepartmentRequest request, int tenantId)
    {
        var department = await context.Departments
            .FirstOrDefaultAsync(d => d.ExternalId == id && d.TenantId == tenantId)
            ?? throw new AppException(404, "Department not found", "DEPT_NOT_FOUND");

        if (request.Name != null && request.Name.ToLower() != department.Name.ToLower())
        {
            if (await context.Departments.AnyAsync(d => d.TenantId == tenantId && d.Name.ToLower() == request.Name.ToLower()))
            {
                throw new AppException(400, "A department with this name already exists.", "DUPLICATE_DEPARTMENT_NAME");
            }
            department.Name = request.Name;
        }

        if (request.Description != null) department.Description = request.Description;
        if (request.IsActive.HasValue) department.IsActive = request.IsActive.Value;

        await context.SaveChangesAsync(CancellationToken.None);
    }

    public async Task DeleteDepartmentAsync(Guid id, int tenantId)
    {
        var department = await context.Departments
            .FirstOrDefaultAsync(d => d.ExternalId == id && d.TenantId == tenantId)
            ?? throw new AppException(404, "Department not found", "DEPT_NOT_FOUND");

        if (await context.Users.AnyAsync(u => u.DepartmentId == department.Id))
        {
            throw new AppException(409, "Cannot delete department with assigned users", "DEPT_HAS_USERS");
        }

        context.Departments.Remove(department);
        await context.SaveChangesAsync(CancellationToken.None);
    }


    public async Task<PaginatedResult<DepartmentManagementResponse>> GetDepartmentManagementAsync(int tenantId, QueryRequest request)
    {
        var today = DateTime.UtcNow.Date;
        var query = context.Departments.Where(d => d.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(d => d.Name.ToLower().Contains(search) || (d.Description != null && d.Description.ToLower().Contains(search)));
        }

        if(request.Filters?.TryGetValue("isActive", out var activeStr) == true && bool.TryParse(activeStr, out var isActive))
        {
            query = query.Where(d => d.IsActive == isActive);
        }

        return await query
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentManagementResponse(
                d.ExternalId,
                d.Name,
                d.Description,
                d.Employees.Count(),
                d.Employees.Count(e => e.LeaveRequests.Any(lr =>
                    lr.Status == LeaveStatus.Approved &&
                    today >= lr.StartDate.Date &&
                    today <= lr.EndDate.Date)),
                d.Employees.Any()
                    ? (double)d.Employees.Count(e => e.LeaveRequests.Any(lr => lr.Status == LeaveStatus.Approved && today >= lr.StartDate.Date && today <= lr.EndDate.Date)) / d.Employees.Count() * 100
                    : 0,
                d.Employees
                    .Where(e => e.LeaveRequests.Any(lr => lr.Status == LeaveStatus.Approved && today >= lr.StartDate.Date && today <= lr.EndDate.Date))
                    .Select(e => e.FirstName + " " + e.LastName)
                    .Take(2)
                    .ToList()
            ))
            .ToPaginatedResultAsync(request);
    }
}
