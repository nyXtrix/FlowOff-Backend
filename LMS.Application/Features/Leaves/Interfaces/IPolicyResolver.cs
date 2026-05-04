using LMS.Domain.Entities.Leave;

namespace LMS.Application.Features.Leaves.Interfaces;

public interface IPolicyResolver
{
    Task<LeaveUsagePolicy> ResolveUsagePolicyAsync(Guid userExternalId, int tenantId);
    Task<WeekOffPolicy> ResolveWeekOffPolicyAsync(Guid userExternalId, int tenantId);
    Task<BalancePolicy> ResolveBalancePolicyAsync(Guid userExternalId, int leaveTypeId, int tenantId);
}