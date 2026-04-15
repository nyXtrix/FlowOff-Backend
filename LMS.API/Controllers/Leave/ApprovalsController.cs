using System.Security.Claims;
using LMS.API.Filters;
using LMS.Application.Features.Leaves.DTOs.Manager;
using LMS.Application.Features.Leaves.Interfaces;
using LMS.Domain.Enums.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.API.Controllers.Leave;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApprovalsController(IApprovalService approvalService) : ControllerBase
{
    [AuthorizePermission("APPROVALS", ActionType.VIEW)]
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        var userExternalId = GetUserExternalId();
        var results = await approvalService.GetPendingApprovalsAsync(userExternalId);
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

    private Guid GetUserExternalId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
}
