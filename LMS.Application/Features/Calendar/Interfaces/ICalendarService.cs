using LMS.Application.Features.Calendar.DTOs;

namespace LMS.Application.Features.Calendar.Interfaces;

public interface ICalendarService
{
    Task<CalendarResponse> GetCalendarAsync(int year, int month, Guid userExternalId, int tenantId, string? filter = "ALL");
    Task CreateHolidayAsync(HolidayCreateRequest request, int tenantId);
    Task UpdateHolidayAsync(Guid externalId, HolidayUpdateRequest request, int tenantId);
    Task DeleteHolidayAsync(Guid externalId, int tenantId);
}
