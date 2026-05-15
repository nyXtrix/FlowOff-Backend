using LMS.Application.Common.Extension;
using LMS.Application.Common.Interfaces;
using LMS.Application.Common.Modals;
using LMS.Application.Features.Leaves.Interfaces;
using LMS.Domain.Entities.Auth;
using LMS.Domain.Entities.Leave;
using LMS.Domain.Entities.Organization;
using LMS.Domain.Enums.Policy;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Leaves.Services;

public class PolicyResolver(IAppDbContext context) : IPolicyResolver
{

    public async Task<LeavePolicy> ResolveUsagePolicyAsync(Guid userExternalId, int tenantId)
    {
        var user = await context.Users
            .Include(u => u.Department)
            .Include(u => u.Role)
            .GetUserByExternalIdAsync(userExternalId);

        var scopes = await context.PolicyScopes
            .Where(s => s.TenantId == tenantId && s.IsActive && s.UsagePolicyId != null)
            .OrderByDescending(s => s.Priority)
            .ToListAsync();

        foreach (var scope in scopes)
        {
            if (IsMatch(scope, user)) 
                return await context.LeavePolicies.FindAsync(scope.UsagePolicyId!) 
                    ?? throw new AppException(404, "Target usage policy not found", "NOT_FOUND");
        }

        throw new AppException(404, "No applicable usage policy found", "NOT_FOUND");
    }

    public async Task<LeavePolicy> ResolveWeekOffPolicyAsync(Guid userExternalId, int tenantId)
    {
        var user = await context.Users
            .Include(u => u.Department)
            .Include(u => u.Role)
            .GetUserByExternalIdAsync(userExternalId);

        var scopes = await context.PolicyScopes
            .Where(s => s.TenantId == tenantId && s.IsActive && s.WeekOffPolicyId != null)
            .OrderByDescending(s => s.Priority)
            .ToListAsync();

        foreach (var scope in scopes)
        {
            if (IsMatch(scope, user)) 
                return await context.LeavePolicies.FindAsync(scope.WeekOffPolicyId!) 
                    ?? throw new AppException(404, "Weekoff policy missing", "NOT_FOUND");
        }

        throw new AppException(404, "No week-off policy found", "NOT_FOUND");
    }

    public async Task<LeavePolicy> ResolveBalancePolicyAsync(Guid userExternalId, int leaveTypeId, int tenantId)
    {
        var user = await context.Users
            .Include(u => u.Department)
            .Include(u => u.Role)
            .GetUserByExternalIdAsync(userExternalId);

        var scopes = await context.PolicyScopes
            .Where(s => s.TenantId == tenantId && s.IsActive && s.BalancePolicyId != null)
            .OrderByDescending(s => s.Priority)
            .ToListAsync();

        foreach (var scope in scopes)
        {
            if (IsMatch(scope, user))
            {
                var policy = await context.LeavePolicies.FindAsync(scope.BalancePolicyId!);

                if (policy != null && policy.LeaveTypeId == leaveTypeId)
                {
                    return policy;
                }
            }
        }

        return await context.LeavePolicies
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.LeaveTypeId == leaveTypeId && p.Type == LeavePolicyType.Balance) 
            ?? throw new AppException(404, "No balance policy found for this leave type", "POLICY_NOT_FOUND");
    }

    private static bool IsMatch(PolicyScopes scope, User user)
    {
        return scope.ScopeType switch
        {
            PolicyScope.Employee => scope.ScopeValue == user.ExternalId.ToString(),
            PolicyScope.Department => scope.ScopeValue == user.Department?.ExternalId.ToString(),
            PolicyScope.Role => scope.ScopeValue == user.Role?.ExternalId.ToString(),
            PolicyScope.Org => true,
            _ => false
        };
    }
}