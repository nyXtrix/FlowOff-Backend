using LMS.Application.Features.Leaves.DTOs.Employee;
using LMS.Application.Features.Leaves.DTOs.Admin;

namespace LMS.Application.Features.Leaves.Interfaces;

public interface ILeaveService
{
    Task<Guid> ApplyLeaveAsync(ApplyLeaveRequest request, Guid userExternalId, int tenantId);
    Task<List<MyLeaveRequestResponse>> GetMyHistoryAsync(Guid userExternalId);
    Task<List<LeaveBalanceResponse>> GetMyBalancesAsync(Guid userExternalId);
    Task CancelRequestAsync(Guid requestExternalId, Guid userExternalId);
    Task<List<HolidayResponse>> GetHolidaysAsync(int tenantId);
    Task<List<LeaveTypeResponse>> GetAvailableLeaveTypesAsync(int tenantId);
}