using LMS.API.Filters;
using LMS.Application.Features.Organization.Department.DTOs;
using LMS.Application.Features.Organization.Department.Interfaces;
using LMS.Domain.Enums.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.API.Controllers.Organization;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentsController(IDepartmentService departmentService) : ControllerBase
{
    [AuthorizePermission("ORG_MGMT", ActionType.CREATE)]
    [HttpPost]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentRequest request)
    {
        var tenantId = int.Parse(User.FindFirst("TenantId")?.Value ?? "0");
        var result = await departmentService.CreateDepartmentAsync(request, tenantId);
        return Ok(result);
    }

    [AuthorizePermission("ORG_MGMT", ActionType.VIEW)]
    [HttpGet]
    public async Task<IActionResult> GetDepartments()
    {
        var tenantId = int.Parse(User.FindFirst("TenantId")?.Value ?? "0");
        var result = await departmentService.GetDepartmentsAsync(tenantId);
        return Ok(result);
    }
}
