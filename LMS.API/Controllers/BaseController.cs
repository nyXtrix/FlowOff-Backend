using System.Security.Claims;
using LMS.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LMS.API.Controllers;

[ApiController]
public abstract class BaseController(IAppDbContext context) : ControllerBase
{
    protected Guid GetUserExternalId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException("User identity missing"));

    protected async Task<int> GetTenantIdAsync()
    {
        var externalId = Guid.Parse(User.FindFirst("TenantExternalId")?.Value ?? throw new UnauthorizedAccessException("User tenant identity missing"));

        var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.ExternalId == externalId) ?? throw new UnauthorizedAccessException("Tenant not found");

        return tenant.Id;
    }
}