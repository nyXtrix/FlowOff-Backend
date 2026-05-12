using LMS.API.Filters;
using LMS.Application.Common.Interfaces;
using LMS.Application.Common.Modals;
using LMS.Application.Features.Organization.Employees.Services;
using LMS.Domain.Enums.Authorization;
using LMS.Domain.Enums.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;

namespace LMS.API.Controllers.Organization;

[Authorize]
[Route("api/v1/organization/employees/bulk-invite")]
[ApiController]
[EnableRateLimiting("fixed")]
public class BulkUserInviteController(BulkUserInviteService bulkService, IAppDbContext context) : BaseController(context)
{
    private readonly BulkUserInviteService _bulkService = bulkService;
    private readonly IAppDbContext _context = context;

    [AuthorizePermission("EMPLOYEE_MGMT", ActionType.CREATE)]
    [HttpPost("upload")]
    public async Task<IActionResult> UploadBulkInvite(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var tenantId = await GetTenantIdAsync();

        var activeInvite = await _bulkService.GetActiveBulkInviteAsync(tenantId);
        if (activeInvite != null)
            throw new AppException(
                400,
                "A bulk upload is already in progress. Please wait for it to complete.",
                "UPLOAD_IN_PROGRESS");

        var tenant = await _context.Tenants.FindAsync(tenantId);

        if (tenant == null || tenant.RemainingDailyInvites <= 0)
            throw new AppException(
                400,
                "Your daily invitation quota has been reached. Please try again tomorrow.",
                "QUOTA_EXCEEDED");

        using var stream = file.OpenReadStream();

        var externalId = await _bulkService.UploadFromFileAsync(
            stream,
            file.FileName,
            tenantId);

        return Ok(new
        {
            bulkInvitedId = externalId,
            Message = "Upload successful. Background processing started."
        });
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveBulkInvite()
    {
        var tenantId = await GetTenantIdAsync();
        var externalId = await _bulkService.GetActiveBulkInviteAsync(tenantId);
        return Ok(new { externalId });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetBulkInviteHistory()
    {
        var tenantId = await GetTenantIdAsync();
        return Ok(await _bulkService.GetHistoryAsync(tenantId));
    }

    [HttpGet("details/{externalId}")]
    public async Task<IActionResult> GetBulkInviteDetails(Guid externalId)
    {
        return Ok(await _bulkService.GetBulkInviteDetailsAsync(externalId));
    }

    [HttpGet("status/{bulkInvitedId}")]
    public async Task<IActionResult> GetBulkInviteStatus(Guid bulkInvitedId)
    {
        return Ok(await _bulkService.GetStatusAsync(bulkInvitedId));
    }
}
