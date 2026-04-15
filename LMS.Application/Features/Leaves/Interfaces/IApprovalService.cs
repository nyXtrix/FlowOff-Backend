using LMS.Application.Features.Leaves.DTOs.Manager;

namespace LMS.Application.Features.Leaves.Interfaces;

public interface IApprovalService
{
    Task<List<PendingApprovalResponse>> GetPendingApprovalsAsync(Guid userExternalId);
    Task ProcessApprovalAsync(ProcessApprovalRequest request, Guid userExternalId);
    Task ForwardApprovalAsync(ForwardApprovalRequest request, Guid userExternalId);
}