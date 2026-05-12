using LMS.API.Filters;
using LMS.Application.Common.DTOs;
using LMS.Application.Common.Interfaces;
using LMS.Application.Features.Organization.Team.Interfaces;
using LMS.Domain.Enums.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.API.Controllers.Organization;

[ApiController]
[Route("api/v1/organization/team")]
public class TeamController(ITeamService teamService, IAppDbContext context) : BaseController(context)
{
    [AuthorizePermission("TEAM", ActionType.VIEW)]
    [HttpGet]
    public async Task<IActionResult> GetMyTeam([FromQuery] QueryRequest request)
    {
        var tenantId = await GetTenantIdAsync();
        var userExternalId = GetUserExternalId();

        var result = await teamService.GetTeamAsync(userExternalId, tenantId, request);
        return Ok(result);
    }
}