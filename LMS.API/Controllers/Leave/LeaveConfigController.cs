using LMS.API.Filters;
using LMS.Application.Features.Leaves.DTOs.Admin;
using LMS.Application.Features.Leaves.Interfaces;
using LMS.Domain.Enums.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.API.Controllers.Leave;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveConfigController(ILeaveConfigService configService) : ControllerBase
{
    [AuthorizePermission("LEAVE_MGMT", ActionType.CREATE)]
    [HttpPost("types")]
    public async Task<IActionResult> CreateType([FromBody] CreateLeaveTypeRequest request)
    {
        var tenantId = GetTenantId();
        var externalId = await configService.CreateLeaveTypeAsync(request, tenantId);
        return Ok(new { message = "Leave type created successfully", id = externalId });
    }

    [AuthorizePermission("POLICY", ActionType.CREATE)]
    [HttpPost("holidays")]
    public async Task<IActionResult> CreateHoliday([FromBody] CreateHolidayRequest request)
    {
        var tenantId = GetTenantId();
        var externalId = await configService.CreateHolidayAsync(request, tenantId);
        return Ok(new { message = "Holiday created successfully", id = externalId });
    }

    [AuthorizePermission("POLICY", ActionType.DELETE)]
    [HttpDelete("holidays/{id}")]
    public async Task<IActionResult> DeleteHoliday(Guid id)
    {
        var tenantId = GetTenantId();
        await configService.DeleteHolidayAsync(id, tenantId);
        return Ok(new { message = "Holiday deleted successfully" });
    }

    [AuthorizePermission("ROLE_MGMT", ActionType.CREATE)]
    [HttpPost("rules")]
    public async Task<IActionResult> CreateRule([FromBody] CreateWorkflowRuleRequest request)
    {
        var tenantId = GetTenantId();
        var externalId = await configService.CreateWorkflowRuleAsync(request, tenantId);
        return Ok(new { message = "Workflow rule created successfully", id = externalId });
    }

    [AuthorizePermission("ROLE_MGMT", ActionType.VIEW)]
    [HttpGet("rules")]
    public async Task<IActionResult> GetRules()
    {
        var tenantId = GetTenantId();
        var results = await configService.GetWorkflowRulesAsync(tenantId);
        return Ok(results);
    }

    [AuthorizePermission("ROLE_MGMT", ActionType.DELETE)]
    [HttpDelete("rules/{id}")]
    public async Task<IActionResult> DeleteRule(Guid id)
    {
        var tenantId = GetTenantId();
        await configService.DeleteWorkflowRuleAsync(id, tenantId);
        return Ok(new { message = "Workflow rule deleted successfully" });
    }

    private int GetTenantId() => int.Parse(User.FindFirst("TenantId")?.Value ?? "0");
}
