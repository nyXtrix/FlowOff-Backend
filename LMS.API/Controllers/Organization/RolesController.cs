using LMS.API.Filters;
using LMS.Application.Common.DTOs;
using LMS.Application.Common.Interfaces;
using LMS.Application.Features.Organization.Roles.DTOs;
using LMS.Application.Features.Organization.Roles.Interfaces;
using LMS.Domain.Enums.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.API.Controllers.Organization;

[ApiController]
[Route("api/v1/organization/[controller]")]
[Authorize]
public class RolesController(IRoleService roleService, IAppDbContext context) : BaseController(context)
{
    [AuthorizePermission("ORGANIZATION", ActionType.CREATE)]
    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        var tenantId = await GetTenantIdAsync();
        var id = await roleService.CreateRoleAsync(request, tenantId);

        return CreatedAtAction(nameof(GetRoles), new { id }, id);
    }

    [AuthorizePermission("ORGANIZATION", ActionType.UPDATE)]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request)
    {
        var tenantId = await GetTenantIdAsync();
        await roleService.UpdateRoleAsync(id, request, tenantId);

        return NoContent();
    }

    [AuthorizePermission("ORGANIZATION", ActionType.DELETE)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        var tenantId = await GetTenantIdAsync();

        await roleService.DeleteRoleAsync(id, tenantId);

        return NoContent();
    }

    [AuthorizePermission("ORGANIZATION", ActionType.VIEW)]
    [HttpGet]
    public async Task<IActionResult> GetRoles([FromQuery] QueryRequest request)
    {
        var tenantId = await GetTenantIdAsync();
        return Ok(await roleService.GetRolesAsync(tenantId, request));
    }


    [AuthorizePermission("ORGANIZATION", ActionType.UPDATE)]
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> RoleStatus(Guid id, [FromBody] RoleStatusRequest request)
    {
        var tenantId = await GetTenantIdAsync();
        await roleService.RoleStatusAsync(id, request.IsActive, tenantId);
        return Ok(new { message = "Role status updated successfully" });
    }
}