using LMS.Application.Features.Leaves.DTOs.Employee;
using LMS.Application.Features.Leaves.DTOs.Admin;
using LMS.Application.Common.DTOs;

namespace LMS.Application.Features.Leaves.Interfaces;

public interface ILeaveService
{
    Task<Guid> ApplyLeaveAsync(ApplyLeaveRequest request, Guid userExternalId, int tenantId);
    Task<PaginatedResult<MyLeaveRequestResponse>> GetMyHistoryAsync(Guid userExternalId, QueryRequest request);
    Task<List<LeaveBalanceResponse>> GetMyBalancesAsync(Guid userExternalId);
    Task CancelRequestAsync(Guid requestExternalId, Guid userExternalId);
    Task<List<HolidayResponse>> GetHolidaysAsync(int tenantId);
    Task<List<LeaveTypeResponse>> GetAvailableLeaveTypesAsync(int tenantId);
    Task<decimal> CalculateActualDaysAsync(DateTime startDate, DateTime endDate, Guid userExternalId, int tenantId);
}