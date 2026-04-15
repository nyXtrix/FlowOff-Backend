using LMS.Application.Common.Interfaces;
using LMS.Application.Features.Employees.DTOs;
using LMS.Application.Features.Employees.Interfaces;
using LMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Employees.Services;

public class EmployeeService(IAppDbContext context) : IEmployeeService
{
    public async Task<List<EmployeeLookupResponse>> GetEmployeeLookupsAsync(string query, int tenantId)
    {
        var userQuery = context.Users.Where(u => u.TenantId == tenantId && u.Status == UserStatus.Activated);

        if (!string.IsNullOrWhiteSpace(query))
        {
            query = query.ToLower().Trim();
            userQuery = userQuery.Where(u => u.FirstName.ToLower().Contains(query) || u.LastName.ToLower().Contains(query) || u.Email.ToLower().Contains(query));

        }
        return await userQuery.Select(u => new EmployeeLookupResponse(u.FirstName + " " + u.LastName, u.ExternalId)).Take(20).ToListAsync();
    }
}