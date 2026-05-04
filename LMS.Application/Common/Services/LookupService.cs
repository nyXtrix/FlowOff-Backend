using LMS.Application.Common.Interfaces;
using LMS.Domain.Enums;
using LMS.Domain.Enums.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Common.Services;

public class LookupService(IAppDbContext context) : ILookupService
{
    public async Task<List<LookupResponse>> GetGenderLookupAsync()
    {
        return await context.Genders.OrderBy(g => g.Value)
                     .Select(g => new LookupResponse(g.Name, g.Value.ToString())).ToListAsync();
    }

    public async Task<List<LookupResponse>> GetLeaveTypeLookupAsync(int tenantId)
    {
        return await context.LeaveTypes.Where(l => l.TenantId == tenantId).Select(l => new LookupResponse(l.Name, l.ExternalId.ToString())).ToListAsync();
    }

    public async Task<List<LookupResponse>> GetDepartmentLookupAsync(int tenantId)
    {
        return await context.Departments.Where(d => d.TenantId == tenantId && d.IsActive)
                     .Select(d => new LookupResponse(d.Name, d.ExternalId.ToString())).ToListAsync();
    }

    public async Task<List<LookupResponse>> GetRoleLookupAsync(int tenantId)
    {
        return await context.Roles.Where(r => r.TenantId == tenantId && r.IsActive && r.Type != RoleType.SYSTEM)
                      .Select(r => new LookupResponse(r.Name, r.ExternalId.ToString())).ToListAsync();
    }
}