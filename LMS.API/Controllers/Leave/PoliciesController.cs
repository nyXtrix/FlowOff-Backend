using LMS.API.Filters;
using LMS.Application.Common.Interfaces;
using LMS.Application.Features.Leaves.DTOs.Admin;
using LMS.Application.Features.Leaves.Interfaces;
using LMS.Domain.Enums.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.RateLimiting;

namespace LMS.API.Controllers.Leave;

[ApiController]
[Route("api/v1/leave/[controller]")]
[Authorize]
[EnableRateLimiting("fixed")]
public class PoliciesController(IPolicyService policyService, IAppDbContext context) : BaseController(context)
{
    [HttpPost]
    [AuthorizePermission("POLICY", ActionType.CREATE)]
    public async Task<IActionResult> Create([FromBody] CreatePolicyRequest request)
    {
        var tenantId = await GetTenantIdAsync();
        var externalId = await policyService.CreatePolicyAsync(request, tenantId);
        
        return Ok(new { 
            message = "Policy created successfully", 
            id = externalId 
        });
    }

    [HttpGet]
    [AuthorizePermission("POLICY", ActionType.VIEW)]
    public async Task<IActionResult> GetAll()
    {
        var tenantId = await GetTenantIdAsync();
        var policies = await policyService.GetAllPoliciesAsync(tenantId);
        return Ok(policies);
    }

    [HttpGet("{scopeType}/{scopeValue}")]
    [AuthorizePermission("POLICY", ActionType.VIEW)]
    public async Task<IActionResult> GetByScope(string scopeType, string scopeValue)
    {
        var tenantId = await GetTenantIdAsync();
        
        if (!Enum.TryParse<LMS.Domain.Enums.Policy.PolicyScope>(scopeType, true, out var scopeTypeEnum))
        {
            return BadRequest("Invalid scope type");
        }

        var policy = await policyService.GetPolicyByScopeAsync(scopeTypeEnum, scopeValue, tenantId);
        
        if (policy == null) return NotFound();
        
        return Ok(policy);
    }
}
