using LMS.API.Filters;
using LMS.Application.Features.Employees.Interfaces;
using LMS.Domain.Enums.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.API.Controllers.Organization;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController(IEmployeeService employeeService) : ControllerBase
{
    [AuthorizePermission("EMPLOYEE_MGMT", ActionType.VIEW)]
    [HttpGet("lookup")]
    public async Task<IActionResult> EmployeeLookup([FromQuery] string? q)
    {
        var tenantId = int.Parse(User.FindFirst("TenantId")?.Value ?? "0");

        var result = await employeeService.GetEmployeeLookupsAsync(q ?? "", tenantId);

        return Ok(result);
    }
}