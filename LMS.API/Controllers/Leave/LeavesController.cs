using System.Security.Claims;
using LMS.API.Filters;
using LMS.Application.Common.Interfaces;
using LMS.Application.Features.Leaves.DTOs.Employee;
using LMS.Application.Features.Leaves.Interfaces;
using LMS.Domain.Enums.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.API.Controllers.Leave;

[ApiController]
[Route("api/v1/leave/[controller]")]
[Authorize]
public class LeavesController(ILeaveService leaveService, IAppDbContext context) : BaseController(context)
{
    [AuthorizePermission("MY_LEAVES", ActionType.CREATE)]
    [HttpPost("apply")]
    public async Task<IActionResult> Apply([FromBody] ApplyLeaveRequest request)
    {
        var userExternalId = GetUserExternalId();
        var tenantId = await GetTenantIdAsync();
        var externalId = await leaveService.ApplyLeaveAsync(request, userExternalId, tenantId);
        return Ok(new { message = "Leave request submitted successfully", id = externalId });
    }

    [AuthorizePermission("MY_LEAVES", ActionType.VIEW)]
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var userExternalId = GetUserExternalId();
        var results = await leaveService.GetMyHistoryAsync(userExternalId);
        return Ok(results);
    }

    [AuthorizePermission("MY_LEAVES", ActionType.VIEW)]
    [HttpGet("balances")]
    public async Task<IActionResult> GetBalances()
    {
        var userExternalId = GetUserExternalId();
        var results = await leaveService.GetMyBalancesAsync(userExternalId);
        return Ok(results);
    }

    [AuthorizePermission("MY_LEAVES", ActionType.DELETE)]
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var userExternalId = GetUserExternalId();
        await leaveService.CancelRequestAsync(id, userExternalId);
        return Ok(new { message = "Leave request cancelled successfully" });
    }

    [AuthorizePermission("POLICY", ActionType.VIEW)]
    [HttpGet("holidays")]
    public async Task<IActionResult> GetHolidays()
    {
        var tenantId = await GetTenantIdAsync();
        var results = await leaveService.GetHolidaysAsync(tenantId);
        return Ok(results);
    }

    [AuthorizePermission("POLICY", ActionType.VIEW)]
    [HttpGet("types")]
    public async Task<IActionResult> GetTypes()
    {
        var tenantId = await GetTenantIdAsync();
        var results = await leaveService.GetAvailableLeaveTypesAsync(tenantId);
        return Ok(results);
    }

}
