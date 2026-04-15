using LMS.Application.Common.Interfaces;
using LMS.Application.Features.Organization.Department.DTOs;
using LMS.Application.Features.Organization.Department.Interfaces;
using LMS.Domain.Entities.Organization;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Organization.Department.Services;

public class DepartmentService(IAppDbContext context) : IDepartmentService
{
    public async Task<Guid> CreateDepartmentAsync(CreateDepartmentRequest request, int tenantId)
    {
        var department = new Domain.Entities.Organization.Department
        {
            Name = request.Name,
            Description = request.Description,
            TenantId = tenantId
        };

        context.Departments.Add(department);
        await context.SaveChangesAsync(CancellationToken.None);

        return department.ExternalId;
    }

    public async Task<List<GetDepartments>> GetDepartmentsAsync(int tenantId)
    {
        return await context.Departments
            .Where(d => d.TenantId == tenantId)
            .Select(d => new GetDepartments(d.Name, d.ExternalId))
            .ToListAsync();
    }
}
