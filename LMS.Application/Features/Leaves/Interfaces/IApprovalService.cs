using LMS.Application.Common.DTOs;
using LMS.Application.Features.Leaves.DTOs.Manager;

namespace LMS.Application.Features.Leaves.Interfaces;

public interface IApprovalService
{
    Task<PaginatedResult<ApprovalListResponse>> GetApprovalsAsync(QueryRequest request, Guid userExternalId);
    Task ProcessApprovalAsync(ProcessApprovalRequest request, Guid userExternalId);
    Task ForwardApprovalAsync(ForwardApprovalRequest request, Guid userExternalId);
    Task<ApprovalStatsResponse> GetApprovalStatsAsync(Guid userExternalId);
}