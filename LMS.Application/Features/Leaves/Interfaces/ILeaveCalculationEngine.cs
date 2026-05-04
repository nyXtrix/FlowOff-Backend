using LMS.Domain.Entities.Leave;

namespace LMS.Application.Features.Leaves.Interfaces;

public interface ILeaveCalculationEngine
{
    Task<decimal> CalculateLeaveDaysAsync(DateTime startDate, DateTime endDate, Guid userExternalId, int tenantId, LeaveUsagePolicy usagePolicy, WeekOffPolicy weekOffPolicy);
}