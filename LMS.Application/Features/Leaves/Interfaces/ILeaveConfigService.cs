using LMS.Application.Features.Leaves.DTOs.Admin;

namespace LMS.Application.Features.Leaves.Interfaces;

public interface ILeaveConfigService
{
    Task<Guid> CreateLeaveTypeAsync(CreateLeaveTypeRequest request, int tenantId);
    Task<Guid> CreateHolidayAsync(CreateHolidayRequest request, int tenantId);
    Task DeleteHolidayAsync(Guid holidayExternalId, int tenantId);
    Task<Guid> CreateWorkflowRuleAsync(CreateWorkflowRuleRequest request, int tenantId);
    Task<List<WorkflowRuleResponse>> GetWorkflowRulesAsync(int tenantId);
    Task DeleteWorkflowRuleAsync(Guid ruleExternalId, int tenantId);
}
