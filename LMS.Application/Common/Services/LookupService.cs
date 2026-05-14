using LMS.Application.Common.Interfaces;
using LMS.Domain.Enums;
using LMS.Domain.Enums.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Common.Services;

public class LookupService(IAppDbContext context, ICacheService cache) : ILookupService
{
    public async Task<List<LookupResponse>> GetGenderLookupAsync()
    {
        const string cacheKey = "lookup_genders";
        var cached = await cache.GetAsync<List<LookupResponse>>(cacheKey);
        if (cached != null) return cached;

        var results = await context.Genders.OrderBy(g => g.Value)
                     .Select(g => new LookupResponse(g.Name, g.Value.ToString())).ToListAsync();

        await cache.SetAsync(cacheKey, results, TimeSpan.FromHours(24));
        return results;
    }

    public async Task<List<LookupResponse>> GetLeaveTypeLookupAsync(int tenantId)
    {
        string cacheKey = $"lookup_leavetypes_{tenantId}";
        var cached = await cache.GetAsync<List<LookupResponse>>(cacheKey);
        if (cached != null) return cached;

        var results = await context.LeaveTypes.Where(l => l.TenantId == tenantId).Select(l => new LookupResponse(l.Name, l.ExternalId.ToString())).ToListAsync();

        await cache.SetAsync(cacheKey, results, TimeSpan.FromHours(1));
        return results;
    }

    public async Task<List<LookupResponse>> GetDepartmentLookupAsync(int tenantId)
    {
        string cacheKey = $"lookup_dept_{tenantId}";
        var cached = await cache.GetAsync<List<LookupResponse>>(cacheKey);
        if (cached != null) return cached;

        var results = await context.Departments.Where(d => d.TenantId == tenantId && d.IsActive)
                     .Select(d => new LookupResponse(d.Name, d.ExternalId.ToString())).ToListAsync();

        await cache.SetAsync(cacheKey, results, TimeSpan.FromHours(1));
        return results;
    }

    public async Task<List<LookupResponse>> GetRoleLookupAsync(int tenantId)
    {
        string cacheKey = $"lookup_role_{tenantId}";
        var cached = await cache.GetAsync<List<LookupResponse>>(cacheKey);
        if (cached != null) return cached;

        var results = await context.Roles.Where(r => r.TenantId == tenantId && r.IsActive && r.Type != RoleType.SYSTEM)
                       .Select(r => new LookupResponse(r.Name, r.ExternalId.ToString())).ToListAsync();

        await cache.SetAsync(cacheKey, results, TimeSpan.FromHours(1));
        return results;
    }
}