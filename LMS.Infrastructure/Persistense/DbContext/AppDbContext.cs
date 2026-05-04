using Microsoft.EntityFrameworkCore;
using LMS.Domain.Entities;
using LMS.Domain.Entities.Auth;
using LMS.Application.Common.Interfaces;
using LMS.Domain.Entities.Leave;
using LMS.Domain.Entities.Workflow;
using LMS.Domain.Entities.Organization;
using LMS.Domain.Enums.Policy;
using LMS.Domain.Entities.Users;
using LMS.Domain.Enums.Authorization;

namespace LMS.Infrastructure.Persistense.DbContext;

public class AppDbContext(DbContextOptions<AppDbContext> options) : Microsoft.EntityFrameworkCore.DbContext(options), IAppDbContext
{
    public DbSet<Tenant> Tenants { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<LeaveRequest> LeaveRequests { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Permissions> Permissions { get; set; } = null!;
    public DbSet<RolePermission> RolePermissions { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    public DbSet<UserPermissionOverride> UserPermissionOverrides { get; set; } = null!;
    public DbSet<UserInvite> UserInvites { get; set; } = null!;
    public DbSet<BulkUserInvite> BulkUserInvites { get; set; } = null!;
    public DbSet<LeaveType> LeaveTypes { get; set; }
    public DbSet<LeaveBalance> LeaveBalances { get; set; }
    public DbSet<WorkflowRule> WorkflowRules { get; set; }
    public DbSet<WorkflowStep> WorkflowSteps { get; set; }
    public DbSet<LeaveApprovalStep> LeaveApprovalSteps { get; set; } = null!;
    public DbSet<Holiday> Holidays { get; set; }
    public DbSet<TenantLead> TenantLeads { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<PolicyScopes> PolicyScopes { get; set; } = null!;
    public DbSet<LeaveUsagePolicy> LeaveUsagePolicies { get; set; } = null!;
    public DbSet<WeekOffPolicy> WeekOffPolicies { get; set; } = null!;
    public DbSet<LeaveAllocationPolicy> LeaveAllocationPolicies { get; set; } = null!;
    public DbSet<BalancePolicy> BalancePolicies { get; set; } = null!;
    public DbSet<ApprovalRule> ApprovalRules { get; set; } = null!;
    public DbSet<ApprovalStep> ApprovalSteps { get; set; } = null!;
    public DbSet<Gender> Genders { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>().HasIndex(t => t.Domain).IsUnique();
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<TenantLead>().HasIndex(l => l.Email).IsUnique();
        modelBuilder.Entity<Role>().HasIndex(r => r.Code).IsUnique();

        modelBuilder.Entity<LeaveRequest>().HasIndex(lr => lr.TenantId);
        modelBuilder.Entity<LeaveBalance>().HasIndex(lb => lb.TenantId);
        modelBuilder.Entity<Role>().HasIndex(r => r.TenantId);
        modelBuilder.Entity<User>().HasIndex(u => u.TenantId);

        modelBuilder.Entity<UserRole>().HasKey(x => new { x.UserId, x.RoleId });

        modelBuilder.Entity<RolePermission>().HasKey(x => new { x.RoleId, x.PermissionId });
        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Permissions)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId);

        modelBuilder.Entity<UserPermissionOverride>().HasKey(x => new { x.UserId, x.PermissionId });
        modelBuilder.Entity<UserPermissionOverride>()
            .HasOne(upo => upo.Permissions)
            .WithMany()
            .HasForeignKey(upo => upo.PermissionId);

        modelBuilder.Entity<User>().HasOne(u => u.Manager).WithMany(u => u.Repotees).HasForeignKey(u => u.ManagerId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserInvite>().HasKey(x => x.Id);
        modelBuilder.Entity<UserInvite>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        modelBuilder.Entity<User>().HasOne(u => u.BulkUserInvite).WithMany().HasForeignKey(u => u.BulkUserInvitedId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<LeaveRequest>().Property(p => p.TotalDays).HasPrecision(18, 2);
        modelBuilder.Entity<LeaveBalance>().Property(p => p.Balance).HasPrecision(18, 2);

        modelBuilder.Entity<Department>()
            .HasMany(d => d.Roles)
            .WithOne(r => r.Department)
            .HasForeignKey(r => r.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Department>()
            .HasMany(d => d.Employees)
            .WithOne(u => u.Department)
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Role>().HasData(new Role
        {
            Id = 1,
            TenantId = null,
            Name = "Super Admin",
            Code = "SUPER_ADMIN",
            Description = "Global system administrator with full access.",
            IsActive = true,
            Type = RoleType.SYSTEM,
            Scope = ScopeType.ALL,
            ExternalId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        modelBuilder.Entity<Gender>().HasData(new Gender
        {
            Id = 1,
            Name = "Male",
            Value = 1,
            ExternalId = Guid.Parse("00000000-0000-0000-0000-000000000010"),
            CreatedAt = new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc)
        },
        new Gender
        {
            Id = 2,
            Name = "Female",
            Value = 2,
            ExternalId = Guid.Parse("00000000-0000-0000-0000-000000000011"),
            CreatedAt = new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc)
        },
        new Gender
        {
            Id = 3,
            Name = "Other",
            Value = 3,
            ExternalId = Guid.Parse("00000000-0000-0000-0000-000000000012"),
            CreatedAt = new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}