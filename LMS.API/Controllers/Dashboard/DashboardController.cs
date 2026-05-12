using LMS.API.Filters;
using LMS.Application.Common.Interfaces;
using LMS.Application.Features.Dashboard.Interfaces;
using LMS.Domain.Enums.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.API.Controllers.Dashboard;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class DashboardController(IDashboardService dashboardService, IAppDbContext context) : BaseController(context)
{
    [AuthorizePermission("DASHBOARD", ActionType.VIEW)]
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetUserDashboard()
    {
        var userExternalId = GetUserExternalId();
        var tenantId = await GetTenantIdAsync();

        var result = await dashboardService.GetUserDashboardAsync(userExternalId, tenantId);
        
        return Ok(result);
    }

    [AuthorizePermission("ADMIN_DASHBOARD", ActionType.VIEW)]
    [HttpGet("admin")]
    public async Task<IActionResult> GetAdminDashboard()
    {
        var tenantId = await GetTenantIdAsync();

        var result = await dashboardService.GetAdminDashboardAsync(tenantId);

        return Ok(result);
    }
}
