using LMS.Domain.Entities.Workflow;

namespace LMS.Application.Interfaces;

public interface ILeaveRequestService
{
    Task<IEnumerable<LeaveRequest>> GetAllLeaveRequestsAsync();
    Task<LeaveRequest?> GetLeaveRequestByIdAsync(int id);
    Task CreateLeaveRequestAsync(LeaveRequest leaveRequest);
    Task UpdateLeaveRequestAsync(LeaveRequest leaveRequest);
    Task DeleteLeaveRequestAsync(int id);
}
