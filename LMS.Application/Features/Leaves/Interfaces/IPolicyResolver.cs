using LMS.Domain.Entities.Leave;

namespace LMS.Application.Features.Leaves.Interfaces;

public interface IPolicyResolver
{
    Task<LeavePolicy> ResolveUsagePolicyAsync(Guid userExternalId, int tenantId);
    Task<LeavePolicy> ResolveWeekOffPolicyAsync(Guid userExternalId, int tenantId);
    Task<LeavePolicy> ResolveBalancePolicyAsync(Guid userExternalId, int leaveTypeId, int tenantId);
}