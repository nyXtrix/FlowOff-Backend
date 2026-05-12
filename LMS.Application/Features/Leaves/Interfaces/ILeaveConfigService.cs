using LMS.Application.Common.DTOs;
using LMS.Application.Features.Leaves.DTOs.Admin;

namespace LMS.Application.Features.Leaves.Interfaces;

public interface ILeaveConfigService
{
    Task<PaginatedResult<LeaveTypeResponse>> GetLeaveTypesAsync(int tenantId, int page, int pageSize);
    Task UpdateLeaveTypeAsync(Guid externalId, CreateLeaveTypeRequest request, int tenantId);
    Task DeleteLeaveTypeAsync(Guid externalId, int tenantId);
    Task<Guid> CreateLeaveTypeAsync(CreateLeaveTypeRequest request, int tenantId);
    Task<Guid> CreateHolidayAsync(CreateHolidayRequest request, int tenantId);
    Task DeleteHolidayAsync(Guid holidayExternalId, int tenantId);
    Task<Guid> CreateWorkflowRuleAsync(CreateWorkflowRuleRequest request, int tenantId);
    Task<List<WorkflowRuleResponse>> GetWorkflowRulesAsync(int tenantId);
    Task DeleteWorkflowRuleAsync(Guid ruleExternalId, int tenantId);
}
