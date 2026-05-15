using LMS.Domain.Entities;
using LMS.Domain.Enums.Authorization;
using LMS.Domain.Entities.Leave;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LMS.Infrastructure.Persistense.DbContext;

public static class DbSeeder
{
    public static async Task SeedHolidaysAsync(AppDbContext context)
    {
        var tenants = await context.Tenants.ToListAsync();
        if (!tenants.Any()) return;

        var holidays2026 = new List<(string Name, DateTime Date)>
        {
            ("New Year's Day", new DateTime(2026, 1, 1)),
            ("Republic Day", new DateTime(2026, 1, 26)),
            ("Maha Shivaratri", new DateTime(2026, 2, 15)),
            ("Holi", new DateTime(2026, 3, 4)),
            ("Good Friday", new DateTime(2026, 4, 3)),
            ("Eid-ul-Fitr", new DateTime(2026, 3, 20)),
            ("Labor Day", new DateTime(2026, 5, 1)),
            ("Independence Day", new DateTime(2026, 8, 15)),
            ("Ganesh Chaturthi", new DateTime(2026, 9, 14)),
            ("Gandhi Jayanti", new DateTime(2026, 10, 2)),
            ("Dussehra", new DateTime(2026, 10, 21)),
            ("Diwali", new DateTime(2026, 11, 8)),
            ("Christmas Day", new DateTime(2026, 12, 25))
        };

        foreach (var tenant in tenants)
        {
            var existingHolidays = await context.Holidays
                .Where(h => h.TenantId == tenant.Id && h.Date.Year == 2026)
                .Select(h => h.Name)
                .ToListAsync();

            var newHolidays = holidays2026
                .Where(h => !existingHolidays.Contains(h.Name))
                .Select(h => new Holiday
                {
                    TenantId = tenant.Id,
                    Name = h.Name,
                    Date = DateTime.SpecifyKind(h.Date, DateTimeKind.Utc)
                })
                .ToList();

            if (newHolidays.Any())
            {
                await context.Holidays.AddRangeAsync(newHolidays);
            }
        }

        await context.SaveChangesAsync();
    }

    public static async Task SeedRolesAsync(AppDbContext context)
    {
        if (!await context.Roles.AnyAsync(r => r.Code == "SUPER_ADMIN"))
        {
            var allPermissions = new List<string>
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

            await context.Roles.AddAsync(new Role
            {
                Name = "Super Admin",
                Code = "SUPER_ADMIN",
                Description = "Global system administrator with full access.",
                IsActive = true,
                Type = RoleType.SYSTEM,
                Scope = ScopeType.ALL,
                ExternalId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                PermissionsJson = JsonSerializer.Serialize(allPermissions)
            });
            await context.SaveChangesAsync();
        }
    }

    public static async Task ResetDatabaseAsync(AppDbContext context)
    {
        var tableNames = new[]
        {
            "Notifications",
            "LeaveApprovalSteps",
            "LeaveRequests",
            "LeaveBalances",
            "UserInvites",
            "BulkUserInvites",
            "WorkflowSteps",
            "WorkflowRules",
            "PolicyScopes",
            "LeavePolicies",
            "LeaveTypes",
            "Departments",
            "Holidays",
            "Users",
            "Tenants",
            "TenantLeads"
        };

        foreach (var table in tableNames)
        {
            await context.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE \"{table}\" RESTART IDENTITY CASCADE;");
        }

        await context.Database.ExecuteSqlRawAsync("DELETE FROM \"Roles\" WHERE \"Code\" != 'SUPER_ADMIN';");
        await context.Database.ExecuteSqlRawAsync("ALTER SEQUENCE \"Roles_Id_seq\" RESTART WITH 2;");
    }

    public static async Task FullResetAsync(AppDbContext context)
    {
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }
}