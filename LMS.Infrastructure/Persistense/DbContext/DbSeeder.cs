using LMS.Domain.Entities;
using LMS.Domain.Enums.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Persistense.DbContext;

public static class DbSeeder
{
    public static async Task SeedPermissionsAsync(AppDbContext context)
    {
        var permissionNames = new List<string>
        {
            "DASHBOARD.VIEW",
            "MY_LEAVES.VIEW", "MY_LEAVES.CREATE", "MY_LEAVES.DELETE",
            "POLICY.VIEW", "POLICY.CREATE", "POLICY.UPDATE", "POLICY.DELETE",
            "LEAVE_MGMT.VIEW", "LEAVE_MGMT.CREATE", "LEAVE_MGMT.UPDATE", "LEAVE_MGMT.DELETE",
            "EMPLOYEE_MGMT.VIEW", "EMPLOYEE_MGMT.CREATE", "EMPLOYEE_MGMT.UPDATE", "EMPLOYEE_MGMT.DELETE",
            "ORGANIZATION.VIEW", "ORGANIZATION.UPDATE",
            "APPROVALS.VIEW", "APPROVALS.APPROVE", "APPROVALS.REJECT",
            "ROLE_MGMT.VIEW", "ROLE_MGMT.CREATE", "ROLE_MGMT.UPDATE", "ROLE_MGMT.DELETE",
            "PROFILE.VIEW", "PROFILE.UPDATE",
            "CALENDAR.VIEW", "CALENDAR.CREATE", "CALENDAR.UPDATE", "CALENDAR.DELETE",
            "ADMIN_DASHBOARD.VIEW"
        };

        var existingPermissions = await context.Permissions.Select(p => p.Name).ToListAsync();
        var newPermissions = permissionNames
            .Where(name => !existingPermissions.Contains(name))
            .Select(name => new Permissions { Name = name })
            .ToList();

        if (newPermissions.Any())
        {
            await context.Permissions.AddRangeAsync(newPermissions);
            await context.SaveChangesAsync();
        }
    }
}