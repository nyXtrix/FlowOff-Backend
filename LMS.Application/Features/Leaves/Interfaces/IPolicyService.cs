using LMS.Application.Features.Leaves.DTOs.Admin;

namespace LMS.Application.Features.Leaves.Interfaces;

public interface IPolicyService
{
    Task<Guid> CreatePolicyAsync(CreatePolicyRequest request, int tenantId);
    Task<CreatePolicyRequest?> GetPolicyByScopeAsync(LMS.Domain.Enums.Policy.PolicyScope scopeType, string scopeValue, int tenantId);
    Task<List<PolicySummaryDto>> GetAllPoliciesAsync(int tenantId);
}