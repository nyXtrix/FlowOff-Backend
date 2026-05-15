using System.Globalization;
using System.Text.Json;
using LMS.Application.Common.Interfaces;
using LMS.Application.Common.Modals;
using LMS.Application.Features.Calendar.DTOs;
using LMS.Application.Features.Calendar.Interfaces;
using LMS.Application.Features.Leaves.Interfaces;
using LMS.Application.Features.Leaves.DTOs.Admin;
using LMS.Domain.Entities.Leave;
using LMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Calendar.Services;

public class CalendarService(IAppDbContext context, IPolicyResolver policyResolver) : ICalendarService
{
    public async Task<CalendarResponse> GetCalendarAsync(int year, int month, Guid userExternalId, int tenantId, string? filter = "ALL")
    {
        var user = await context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.ExternalId == userExternalId)
            ?? throw new Exception("User not found");

        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);

        var response = new CalendarResponse();
        filter = filter?.ToUpper() ?? "ALL";
        
        var weekOffDays = new List<int>();
        var totalWeekOffs = 0;

        try
        {
            var weekOffPolicy = await policyResolver.ResolveWeekOffPolicyAsync(userExternalId, tenantId);
            
            var config = JsonSerializer.Deserialize<JsonElement>(weekOffPolicy.ConfigJson);
            var rulesJson = config.TryGetProperty("Rules", out var r) ? r.GetRawText() : "[]";
            var rules = JsonSerializer.Deserialize<List<WeekOffRuleDto>>(rulesJson);

            if (rules != null)
            {
                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    var dayName = date.DayOfWeek.ToString();
                    var weekOfMonth = (date.Day - 1) / 7 + 1;

                    var rule = rules.FirstOrDefault(r => NormalizeDayName(r.Day).Equals(dayName, StringComparison.OrdinalIgnoreCase));
                    bool isWeekOff = rule != null && (rule.Weeks == null || rule.Weeks.Count == 0 || rule.Weeks.Contains(0) || rule.Weeks.Contains(weekOfMonth));
                    if (isWeekOff)
                    {
                        response.Items.Add(new CalendarItem
                        {
                            Id = $"wo-{date:yyyy-MM-dd}",
                            Date = date.ToString("yyyy-MM-dd"),
                            Title = "Weekend",
                            Type = "WEEKOFF",
                            Description = date.DayOfWeek.ToString()
                        });
                        totalWeekOffs++;
                        weekOffDays.Add((int)date.DayOfWeek);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (ex is AppException appEx && appEx.ErrorCode != "NOT_FOUND") throw;
        }

        response.Config.DefaultWeekOffs = weekOffDays.Distinct().ToList();

        var holidays = new List<Holiday>();
        if (filter == "ALL" || filter == "HOLIDAY")
        {
            holidays = await context.Holidays
                .Where(h => h.TenantId == tenantId && h.Date >= startDate && h.Date <= endDate)
                .ToListAsync();

            foreach (var holiday in holidays)
            {
                response.Items.Add(new CalendarItem
                {
                    Id = $"h-{holiday.ExternalId}",
                    Date = holiday.Date.ToString("yyyy-MM-dd"),
                    Title = holiday.Name,
                    Type = "HOLIDAY",
                    Description = "Public Holiday"
                });
            }
        }

        var totalUserLeaves = 0;
        if (filter == "ALL" || filter == "LEAVE")
        {
            var userLeaves = await context.LeaveRequests
                .Include(l => l.LeaveType)
                .Where(l => l.UserId == user.Id && l.Status == LeaveStatus.Approved && 
                            ((l.StartDate >= startDate && l.StartDate <= endDate) || 
                             (l.EndDate >= startDate && l.EndDate <= endDate) ||
                             (l.StartDate <= startDate && l.EndDate >= endDate)))
                .ToListAsync();

            foreach (var leave in userLeaves)
            {
                var leaveStart = leave.StartDate < startDate ? startDate : leave.StartDate;
                var leaveEnd = leave.EndDate > endDate ? endDate : leave.EndDate;

                for (var date = leaveStart.Date; date <= leaveEnd.Date; date = date.AddDays(1))
                {
                    if (date < startDate || date > endDate) continue;

                    response.Items.Add(new CalendarItem
                    {
                        Id = $"l-{user.FirstName.ToLower()}-{leave.ExternalId}-{date:dd}",
                        Date = date.ToString("yyyy-MM-dd"),
                        Title = $"{user.FirstName} {user.LastName}",
                        Type = "LEAVE",
                        SubType = leave.LeaveType.Name.ToUpper(),
                        Description = leave.Reason,
                        Avatar = $"https://ui-avatars.com/api/?name={user.FirstName}+{user.LastName}&background=38bdf8&color=fff",
                        Metadata = new Dictionary<string, string>
                        {
                            { "employeeId", user.ExternalId.ToString() },
                            { "requestId", leave.ExternalId.ToString() }
                        }
                    });
                    totalUserLeaves++;
                }
            }
        }

        var reporteeLeavesCount = 0;
        if (filter == "ALL" || filter == "REPORTEES")
        {
            var reportees = await context.Users
                .Where(u => u.ManagerId == user.Id)
                .Select(u => new { u.Id, u.ExternalId, u.FirstName, u.LastName })
                .ToListAsync();

            var reporteeIds = reportees.Select(r => r.Id).ToList();
            
            var reporteeLeaves = await context.LeaveRequests
                .Include(l => l.LeaveType)
                .Where(l => reporteeIds.Contains(l.UserId) && l.Status == LeaveStatus.Approved &&
                            ((l.StartDate >= startDate && l.StartDate <= endDate) || 
                             (l.EndDate >= startDate && l.EndDate <= endDate) ||
                             (l.StartDate <= startDate && l.EndDate >= endDate)))
                .ToListAsync();

            foreach (var leave in reporteeLeaves)
            {
                var reportee = reportees.First(r => r.Id == leave.UserId);
                var leaveStart = leave.StartDate < startDate ? startDate : leave.StartDate;
                var leaveEnd = leave.EndDate > endDate ? endDate : leave.EndDate;

                for (var date = leaveStart.Date; date <= leaveEnd.Date; date = date.AddDays(1))
                {
                    if (date < startDate || date > endDate) continue;

                    response.Items.Add(new CalendarItem
                    {
                        Id = $"rl-{reportee.FirstName.ToLower()}-{leave.ExternalId}-{date:dd}",
                        Date = date.ToString("yyyy-MM-dd"),
                        Title = $"{reportee.FirstName} {reportee.LastName}",
                        Type = "REPORTEE_LEAVE",
                        SubType = leave.LeaveType.Name.ToUpper(),
                        Description = leave.Reason,
                        Avatar = $"https://ui-avatars.com/api/?name={reportee.FirstName}+{reportee.LastName}&background=f472b6&color=fff",
                        Metadata = new Dictionary<string, string>
                        {
                            { "employeeId", reportee.ExternalId.ToString() },
                            { "requestId", leave.ExternalId.ToString() }
                        }
                    });
                    reporteeLeavesCount++;
                }
            }
        }

        var holidayDates = holidays.Select(h => DateTime.SpecifyKind(h.Date.Date, DateTimeKind.Utc)).Distinct().ToList();
        
        var workingDays = 0;
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var currentDay = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
            if (!weekOffDays.Contains((int)date.DayOfWeek) && !holidayDates.Contains(currentDay))
            {
                workingDays++;
            }
        }

        response.Summary.Month = startDate.ToString("MMMM yyyy");
        response.Summary.TotalHolidays = holidays.Count;
        response.Summary.TotalLeaves = totalUserLeaves + reporteeLeavesCount;
        response.Summary.TotalWeekOffs = totalWeekOffs;
        response.Summary.WorkingDays = workingDays;

        response.Items = response.Items.OrderBy(i => i.Date).ToList();

        return response;
    }

    public async Task CreateHolidayAsync(HolidayCreateRequest request, int tenantId)
    {
        var date = DateTime.ParseExact(request.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var holiday = new Holiday
        {
            TenantId = tenantId,
            Name = request.Name,
            Date = DateTime.SpecifyKind(date, DateTimeKind.Utc)
        };

        context.Holidays.Add(holiday);
        await context.SaveChangesAsync();
    }

    public async Task UpdateHolidayAsync(Guid externalId, HolidayUpdateRequest request, int tenantId)
    {
        var holiday = await context.Holidays
            .FirstOrDefaultAsync(h => h.ExternalId == externalId && h.TenantId == tenantId)
            ?? throw new Exception("Holiday not found");

        var date = DateTime.ParseExact(request.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        holiday.Name = request.Name;
        holiday.Date = DateTime.SpecifyKind(date, DateTimeKind.Utc);

        await context.SaveChangesAsync();
    }

    public async Task DeleteHolidayAsync(Guid externalId, int tenantId)
    {
        var holiday = await context.Holidays
            .FirstOrDefaultAsync(h => h.ExternalId == externalId && h.TenantId == tenantId)
            ?? throw new Exception("Holiday not found");

        context.Holidays.Remove(holiday);
        await context.SaveChangesAsync();
    }

    private static string NormalizeDayName(string day) => day.ToUpper().Trim() switch
    {
        "MON" => "Monday",
        "TUE" => "Tuesday",
        "WED" => "Wednesday",
        "THU" => "Thursday",
        "FRI" => "Friday",
        "SAT" => "Saturday",
        "SUN" => "Sunday",
        _     => day
    };
}
