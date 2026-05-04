using LMS.Domain.Entities.Workflow;

namespace LMS.Application.Features.Leaves.Interfaces;

public interface IApprovalEngine
{
    Task<List<LeaveApprovalStep>> GenerateApprovalChainAsync(Guid userExternalId, int leaveTypeId, decimal totalDays, int tenantId);
}
