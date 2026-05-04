using LMS.Application.Common.Interfaces;
using LMS.Application.Features.Auth.Services.UserInvites;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.API.Controllers.Auth;

[Authorize(Roles = "SUPER_ADMIN")]
[Route("api/v1/auth/[controller]")]
[ApiController]
public class BulkUserInviteController(BulkUserInviteService bulkService, IAppDbContext context) : BaseController(context)
{
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var tenantId = await GetTenantIdAsync();

        using var stream = file.OpenReadStream();
        var externalId = await bulkService.UploadFromFileAsync(stream, file.FileName, tenantId);

        return Ok(new { bulkInvitedId = externalId, Message = "Upload successful. Background processing started." });
    }

    [HttpGet("status/{bulkInvitedId}")]
    public async Task<IActionResult> GetStatus(Guid bulkInvitedId)
    {

        return Ok(await bulkService.GetStatusAsync(bulkInvitedId));
    }
}
