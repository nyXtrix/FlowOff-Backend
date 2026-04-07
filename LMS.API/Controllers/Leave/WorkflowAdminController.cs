using LMS.Application.Features.Leaves.DTOs;
using LMS.Application.Features.Leaves.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.API.Controllers.Leave;

[ApiController]
[Route("api/[controller]")]
[Authorize]

public class WorkflowAdminController(IWorkflowEngine workflowEngine) : ControllerBase
{
    [HttpPost("rules")]
    public async Task<IActionResult> CreateRule([FromBody] CreateWorkflowRuleRequest request)
    {
        var tenantId = int.Parse(User.FindFirst("TenantId")?.Value ?? "0");
        var id = await workflowEngine.CreateWorkflowRuleAsync(request, tenantId);
        return Ok(new { message = "Workflow rule created successfully", id });
    }

    [HttpGet("rules")]
    public async Task<IActionResult> GetRules()
    {
        var tenantId = int.Parse(User.FindFirst("TenantId")?.Value ?? "0");
        var rules = await workflowEngine.GetWorkflowRuleResponsesAsync(tenantId);

        return Ok(rules);
    }

    [HttpDelete("rules/{id}")]
    public async Task<IActionResult> DeleteRule(int id)
    {
        var tenantId = int.Parse(User.FindFirst("TenantId")?.Value ?? "0");
        await workflowEngine.DeleteWorkflowRuleAsync(id, tenantId);

        return Ok(new { message = "Workflow rule deleted succesfully" });
    }

    [HttpPost("holidays")]
    public async Task<IActionResult> CreateHoliday([FromBody] CreateHolidayRequest request)
    {
        var tenantId = int.Parse(User.FindFirst("TenantId")?.Value ?? "0");
        var id = await workflowEngine.CreateHolidayAsync(request, tenantId);

        return Ok(new { message = "Holiday create successfully", id });
    }

    [HttpDelete("holidays/{id}")]
    public async Task<IActionResult> DeleteHoliday(int id)
    {
        var tenantId = int.Parse(User.FindFirst("TenantId")?.Value ?? "0");
        await workflowEngine.DeleteHolidayRequestAsync(id, tenantId);

        return Ok(new { message = "Holiday deleted successfully" });
    }

    [HttpPost("leave-types")]
    public async Task<IActionResult> CreateLeaveType([FromBody] CreateLeaveTypeRequest request)
    {
        var tenantId = int.Parse(User.FindFirst("TenantId")?.Value ?? "0");
        var id = await workflowEngine.CreateLeaveTypeAsync(request, tenantId);

        return Ok(new { message = "Leave type created and balances initialized successfully", id });
    }
}