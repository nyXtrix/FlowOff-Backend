using LMS.Domain.Entities;
using LMS.Domain.Entities.Auth;
using LMS.Domain.Entities.Leave;
using LMS.Domain.Entities.Organization;
using LMS.Domain.Entities.Users;
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
    DbSet<UserInvite> UserInvites { get; set; }
    DbSet<BulkUserInvite> BulkUserInvites { get; set; }
    DbSet<LeaveType> LeaveTypes { get; set; }
    DbSet<LeaveBalance> LeaveBalances { get; set; }
    DbSet<WorkflowRule> WorkflowRules { get; set; }
    DbSet<WorkflowStep> WorkflowSteps { get; set; }
    DbSet<LeaveApprovalStep> LeaveApprovalSteps { get; set; }
    DbSet<Holiday> Holidays { get; set; }
    DbSet<TenantLead> TenantLeads { get; set; }
    DbSet<Department> Departments { get; set; }
    DbSet<PolicyScopes> PolicyScopes { get; set; }
    DbSet<LeavePolicy> LeavePolicies { get; set; }
    DbSet<Notification> Notifications { get; set; }
    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
