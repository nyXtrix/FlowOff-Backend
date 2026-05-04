using LMS.API.Filters;
using LMS.Application.Common.DTOs;
using LMS.Application.Common.Interfaces;
using LMS.Application.Features.Organization.Department.DTOs;
using LMS.Application.Features.Organization.Department.Interfaces;
using LMS.Domain.Enums.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.API.Controllers.Organization;

[ApiController]
[Route("api/v1/organization/[controller]")]
[Authorize]
public class DepartmentsController(IDepartmentService departmentService, IAppDbContext context) : BaseController(context)
{
    [AuthorizePermission("ORGANIZATION", ActionType.CREATE)]
    [HttpPost]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentRequest request)
    {
        var tenantId = await GetTenantIdAsync();
        var result = await departmentService.CreateDepartmentAsync(request, tenantId);
        return Ok(result);
    }

    [AuthorizePermission("ORGANIZATION", ActionType.UPDATE)]
    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateDepartment(Guid id, [FromBody] UpdateDepartmentRequest request)
    {
        var tenantId = await GetTenantIdAsync();
        await departmentService.UpdateDepartmentAsync(id, request, tenantId);
        return NoContent();
    }

    [AuthorizePermission("ORGANIZATION", ActionType.DELETE)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDepartment(Guid id)
    {
        var tenantId = await GetTenantIdAsync();
        await departmentService.DeleteDepartmentAsync(id, tenantId);
        return NoContent();
    }


    [AuthorizePermission("ORGANIZATION", ActionType.VIEW)]
    [HttpGet]
    public async Task<IActionResult> GetDepartments([FromQuery] QueryRequest request)
    {
        var tenantId = await GetTenantIdAsync();
        return Ok(await departmentService.GetDepartmentManagementAsync(tenantId, request));
    }
}
