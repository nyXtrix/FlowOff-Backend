using LMS.Application.Common.Interfaces;
using LMS.Application.Features.Calendar.Interfaces;
using LMS.Application.Features.Calendar.DTOs;
using LMS.API.Filters;
using LMS.Domain.Enums.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CalendarController(ICalendarService calendarService, IAppDbContext context) : BaseController(context)
{
    [HttpGet]
    public async Task<IActionResult> GetCalendar([FromQuery] int year, [FromQuery] int month, [FromQuery] string? filter = "ALL")
    {
        if (year == 0) year = DateTime.UtcNow.Year;
        if (month == 0) month = DateTime.UtcNow.Month;

        var userExtId = GetUserExternalId();
        var tenantId = await GetTenantIdAsync();

        var result = await calendarService.GetCalendarAsync(year, month, userExtId, tenantId, filter);
        return Ok(result);
    }

    [HttpPost("holidays")]
    [AuthorizePermission("CALENDAR", ActionType.CREATE)]
    public async Task<IActionResult> CreateHoliday([FromBody] HolidayCreateRequest request)
    {
        var tenantId = await GetTenantIdAsync();
        await calendarService.CreateHolidayAsync(request, tenantId);
        return Ok(new { message = "Holiday created successfully" });
    }

    [HttpPut("holidays/{id}")]
    [AuthorizePermission("CALENDAR", ActionType.UPDATE)]
    public async Task<IActionResult> UpdateHoliday(Guid id, [FromBody] HolidayUpdateRequest request)
    {
        var tenantId = await GetTenantIdAsync();
        await calendarService.UpdateHolidayAsync(id, request, tenantId);
        return Ok(new { message = "Holiday updated successfully" });
    }

    [HttpDelete("holidays/{id}")]
    [AuthorizePermission("CALENDAR", ActionType.DELETE)]
    public async Task<IActionResult> DeleteHoliday(Guid id)
    {
        var tenantId = await GetTenantIdAsync();
        await calendarService.DeleteHolidayAsync(id, tenantId);
        return Ok(new { message = "Holiday deleted successfully" });
    }
}
