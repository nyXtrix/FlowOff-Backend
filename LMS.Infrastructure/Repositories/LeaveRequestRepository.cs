using LMS.Domain.Entities.Workflow;

namespace LMS.Infrastructure.Repositories;

public class LeaveRequestRepository
{
    private static readonly List<LeaveRequest> _leaveRequests = new();

    public Task<IEnumerable<LeaveRequest>> GetAllAsync() => Task.FromResult<IEnumerable<LeaveRequest>>(_leaveRequests);

    public Task<LeaveRequest?> GetByIdAsync(int id) => Task.FromResult(_leaveRequests.FirstOrDefault(x => x.Id == id));

    public Task AddAsync(LeaveRequest leaveRequest)
    {
        leaveRequest.Id = _leaveRequests.Count + 1;
        _leaveRequests.Add(leaveRequest);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(LeaveRequest leaveRequest)
    {
        var existing = _leaveRequests.FirstOrDefault(x => x.Id == leaveRequest.Id);
        if (existing != null)
        {
            existing.UserId = leaveRequest.UserId;
            existing.LeaveTypeId = leaveRequest.LeaveTypeId;
            existing.StartDate = leaveRequest.StartDate;
            existing.EndDate = leaveRequest.EndDate;
            existing.TotalDays = leaveRequest.TotalDays;
            existing.Status = leaveRequest.Status;
            existing.Reason = leaveRequest.Reason;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id)
    {
        var existing = _leaveRequests.FirstOrDefault(x => x.Id == id);
        if (existing != null)
            _leaveRequests.Remove(existing);
        return Task.CompletedTask;
    }
}
