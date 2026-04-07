using System.Security.Claims;
using LMS.Application.Features.Leaves.DTOs;
using LMS.Application.Features.Leaves.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.API.Controllers.Leave;

[ApiController]
[Route("api/[controller]")]
[Authorize]

public class LeaveManagementController(IWorkflowEngine workflowEngine) : ControllerBase
{
    [HttpPost("apply-leave")]
    public async Task<IActionResult> ApplyLeave([FromBody] ApplyLeaveRequest request)
    {
        var userId = GetCurrentUserId();
        var tenantId = int.Parse(User.FindFirst("TenantId")?.Value ?? "0");
        var leaveId = await workflowEngine.ApplyLeaveRequestAsync(request, userId, tenantId);

        return Ok(new { message = "Leave request submmited for approval", LeaveId = leaveId });
    }

    [HttpPost("approve")]
    public async Task<IActionResult> ApproveLeave(int approvalId, bool isApproved, string remarks)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        await workflowEngine.ProcessApprovalAsync(approvalId, currentUserId, isApproved, remarks);

        return Ok(new { message = isApproved ? "Approved successfully" : "Rejected successfully" });
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(int id)
    {
        var status = await workflowEngine.GetChainStatusAsync(id);

        return Ok(new { LeaveId = id, CurrentStatus = status.ToString() });
    }

    [HttpPost("cancel/{id}")]
    public async Task<IActionResult> CancelLeaveRequest(int id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized("Invalid or missing user ID.");
        }

        await workflowEngine.CancelLeaveRequestAsync(id, userId);

        return Ok(new { message = "Leave request cancelled" });
    }

    [HttpGet("my-requests")]
    public async Task<IActionResult> GetMyLeaveRequest()
    {
        var userId = GetCurrentUserId();

        var requests = await workflowEngine.GetMyRequestsAsync(userId);

        return Ok(requests);
    }

    [HttpGet("approval-pending")]
    public async Task<IActionResult> GetApprovalPendingRequests()
    {
        var userId = GetCurrentUserId();
        return Ok(await workflowEngine.GetPendingApprovalsAsync(userId));
    }

    [HttpGet("leave-types")]
    public async Task<IActionResult> GetLeaveTypes()
    {
        var tenantId = int.Parse(User.FindFirst("TenantId")?.Value ?? "0");

        var types = await workflowEngine.GetLeaveTypesAsync(tenantId);

        return Ok(types);
    }

    [HttpGet("history/{id}")]
    public async Task<IActionResult> GetLeaveHistory(int id)
    {
        return Ok(await workflowEngine.GetApprovalHistoryAsync(id));
    }

    [HttpPost("forward-approval")]
    public async Task<IActionResult> ForwardRequestToNewApprover(int approverId, int nextApproverId)
    {
        var currentUser = GetCurrentUserId();

        await workflowEngine.ForwardToApproverAsync(approverId, currentUser, nextApproverId);

        return Ok(new { message = "Request manually forwarded to next level" });
    }

    [HttpGet("my-leaves")]
    public async Task<IActionResult> GetMyLeaveBalances()
    {
        var userId = GetCurrentUserId();

        var balances = await workflowEngine.GetMyLeaveBalancesAsync(userId);

        return Ok(balances);
    }

    [HttpGet("holidays")]
    public async Task<IActionResult> GetHolidays()
    {
        var tenantId = int.Parse(User.FindFirst("TenantId")?.Value ?? "0");
        var holidays = await workflowEngine.GetHolidaysAsync(tenantId);
        return Ok(holidays);
    }

    private int GetCurrentUserId()
    {
        var user = User.FindFirst(ClaimTypes.NameIdentifier);
        return int.Parse(user?.Value ?? "0");
    }
}
