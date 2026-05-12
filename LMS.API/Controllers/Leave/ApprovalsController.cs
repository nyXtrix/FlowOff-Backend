using System.Security.Claims;
using LMS.API.Filters;
using LMS.Application.Common.DTOs;
using LMS.Application.Common.Interfaces;
using LMS.Application.Features.Leaves.DTOs.Manager;
using LMS.Application.Features.Leaves.Interfaces;
using LMS.Domain.Enums.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LMS.API.Controllers.Leave;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[EnableRateLimiting("fixed")]
public class ApprovalsController(IApprovalService approvalService, IAppDbContext context) : BaseController(context)
{
    [AuthorizePermission("APPROVALS", ActionType.VIEW)]
    [HttpGet]
    public async Task<IActionResult> GetApprovals([FromQuery] QueryRequest request)
    {
        var userExternalId = GetUserExternalId();
        var results = await approvalService.GetApprovalsAsync(request, userExternalId);
        return Ok(results);
    }

    [AuthorizePermission("APPROVALS", ActionType.APPROVE)]
    [HttpPost("process")]
    public async Task<IActionResult> Process([FromBody] ProcessApprovalRequest request)
    {
        var userExternalId = GetUserExternalId();
        await approvalService.ProcessApprovalAsync(request, userExternalId);
        return Ok(new { message = request.IsApproved ? "Approved successfully" : "Rejected successfully" });
    }

    [AuthorizePermission("APPROVALS", ActionType.UPDATE)]
    [HttpPost("forward")]
    public async Task<IActionResult> Forward([FromBody] ForwardApprovalRequest request)
    {
        var userExternalId = GetUserExternalId();
        await approvalService.ForwardApprovalAsync(request, userExternalId);
        return Ok(new { message = "Request forwarded successfully" });
    }

    [AuthorizePermission("APPROVALS", ActionType.VIEW)]
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var userExternalId = GetUserExternalId();
        var results = await approvalService.GetApprovalStatsAsync(userExternalId);
        return Ok(results);
    }
}
