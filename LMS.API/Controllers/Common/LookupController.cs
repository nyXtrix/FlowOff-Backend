using System.Threading.Tasks;
using LMS.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.API.Controllers.Common;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class LookupsController(ILookupService lookupService, IAppDbContext context) : BaseController(context)
{
    [HttpGet("genders")]
    public async Task<IActionResult> GetGenderLookups()
    {
        var results =await lookupService.GetGenderLookupAsync();
        return Ok(results);
    }

    [HttpGet("leave-types")]
    public async Task<IActionResult> GetLeaveTypeLookups()
    {
        var tenantId = await GetTenantIdAsync();
        var results = await lookupService.GetLeaveTypeLookupAsync(tenantId);

        return Ok(results);
    }

    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartmentLookups()
    {
        var tenantId = await GetTenantIdAsync();
        var results = await lookupService.GetDepartmentLookupAsync(tenantId);

        return Ok(results);
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoleLookups()
    {
        var tenantId = await GetTenantIdAsync();
        var results = await lookupService.GetRoleLookupAsync(tenantId);

        return Ok(results);
    }
}