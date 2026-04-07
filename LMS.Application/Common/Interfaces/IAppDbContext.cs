using LMS.Domain.Entities;
using LMS.Domain.Entities.Auth;
using LMS.Domain.Entities.Leave;
using LMS.Domain.Entities.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LMS.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<Tenant> Tenants { get; set; }
    DbSet<User> Users { get; set; }
    DbSet<LeaveRequest> LeaveRequests { get; set; }
    DbSet<Role> Roles { get; set; }
    DbSet<Permissions> Permissions { get; set; }
    DbSet<Position> Positions { get; set; }
    DbSet<RolePermission> RolePermissions { get; set; }
    DbSet<UserRole> UserRoles { get; set; }
    DbSet<UserPermissionOverride> UserPermissionOverrides { get; set; }
    DbSet<UserInvite> UserInvites { get; set; }
    DbSet<LeaveType> LeaveTypes { get; set; }
    DbSet<LeaveBalance> LeaveBalances { get; set; }
    DbSet<WorkflowRule> WorkflowRules { get; set; }
    DbSet<WorkflowStep> WorkflowSteps { get; set; }
    DbSet<LeaveApproval> LeaveApprovals { get; set; }
    DbSet<Holiday> Holidays { get; set; }
    DbSet<TenantLead> TenantLeads { get; set; }
    DatabaseFacade Database { get; }


    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
