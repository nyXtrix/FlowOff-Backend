using Microsoft.EntityFrameworkCore;
using LMS.Domain.Entities;
using LMS.Domain.Entities.Auth;
using LMS.Application.Common.Interfaces;
using LMS.Domain.Entities.Leave;
using LMS.Domain.Entities.Workflow;

namespace LMS.Infrastructure.Persistense.DbContext;

public class AppDbContext(DbContextOptions<AppDbContext> options) : Microsoft.EntityFrameworkCore.DbContext(options), IAppDbContext
{
    public DbSet<Tenant> Tenants { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<LeaveRequest> LeaveRequests { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Permissions> Permissions { get; set; } = null!;
    public DbSet<Position> Positions { get; set; } = null!;
    public DbSet<RolePermission> RolePermissions { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    public DbSet<UserPermissionOverride> UserPermissionOverrides { get; set; } = null!;
    public DbSet<UserInvite> UserInvites { get; set; } = null!;
    public DbSet<LeaveType> LeaveTypes { get; set; }
    public DbSet<LeaveBalance> LeaveBalances { get; set; }
    public DbSet<WorkflowRule> WorkflowRules { get; set; }
    public DbSet<WorkflowStep> WorkflowSteps { get; set; }
    public DbSet<LeaveApproval> LeaveApprovals { get; set; }
    public DbSet<Holiday> Holidays { get; set; }
    public DbSet<TenantLead> TenantLeads { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>().HasIndex(t => t.Domain).IsUnique();

        modelBuilder.Entity<UserRole>().HasKey(x => new { x.UserId, x.RoleId });

        modelBuilder.Entity<RolePermission>().HasKey(x => new { x.RoleId, x.PermissionId });

        modelBuilder.Entity<UserPermissionOverride>().HasKey(x => new { x.UserId, x.PermissionId });

        modelBuilder.Entity<User>().HasOne(u => u.Manager).WithMany(u => u.Repotees).HasForeignKey(u => u.ManagerId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserInvite>().HasKey(x => x.Id);
        modelBuilder.Entity<UserInvite>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);

        modelBuilder.Entity<LeaveRequest>().Property(p => p.TotalDays).HasPrecision(18, 2);
        modelBuilder.Entity<LeaveBalance>().Property(p => p.Balance).HasPrecision(18, 2);
    }
}