using System.Security.Claims;
using LMS.API.Filters;
using LMS.Application.Common.DTOs;
using LMS.Application.Common.Interfaces;
using LMS.Application.Features.Auth.Services;
using LMS.Application.Features.Employees.DTOs;
using LMS.Application.Features.Employees.Interfaces;
using LMS.Domain.Enums.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LMS.API.Controllers.Organization;

[ApiController]
[Route("api/v1/organization/employees")]
[Authorize]
[EnableRateLimiting("fixed")]
public class EmployeesController(IEmployeeService employeeService, IAppDbContext context) : BaseController(context)
{
    [AuthorizePermission("EMPLOYEE_MGMT", ActionType.VIEW)]
    [HttpGet("lookup")]
    public async Task<IActionResult> EmployeeLookup([FromQuery] string? q)
    {
        var tenantId = await GetTenantIdAsync();

        var result = await employeeService.GetEmployeeLookupsAsync(q ?? "", tenantId);

        return Ok(result);
    }

    [AuthorizePermission("EMPLOYEE_MGMT", ActionType.VIEW)]
    [HttpGet]
    public async Task<IActionResult> GetEmployees([FromQuery] QueryRequest request)
    {
        var userExternalId = GetUserExternalId();
        var tenantId = await GetTenantIdAsync();

        var result = await employeeService.GetEmployeesAsync(request, userExternalId, tenantId);

        return Ok(result);
    }

    [AuthorizePermission("EMPLOYEE_MGMT", ActionType.CREATE)]
    [HttpGet("recent-invites")]
    public async Task<IActionResult> GetRecentInvites()
    {
        var tenantId = await GetTenantIdAsync();
        var result = await employeeService.GetRecentInvitesAsync(tenantId);

        return Ok(result);
    }

    [HttpGet("profile/{externalId}")]
    public async Task<IActionResult> GetProfile(Guid externalId)
    {
        var tenantId = await GetTenantIdAsync();

        var userExternalId = GetUserExternalId();
        var result = await employeeService.GetEmployeeProfileAsync(externalId, userExternalId, tenantId);

        return Ok(result);
    }

    [HttpPut("profile/{externalId}")]
    public async Task<IActionResult> UpdateProfile(Guid externalId, [FromBody] UpdateEmployeeRequest request)
    {
        var tenantId = await GetTenantIdAsync();
        var userExternalId = GetUserExternalId();

        await employeeService.UpdateEmployeeProfileAsync(externalId, request, userExternalId, tenantId);

        return Ok(new { message = "Profile updated successfully" });
    }
}