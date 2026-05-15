using System.Text.Json;
using LMS.Application.Common.Interfaces;
using LMS.Application.Features.Leaves.DTOs.Admin;
using LMS.Application.Features.Leaves.Interfaces;
using LMS.Domain.Entities.Leave;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Leaves.Services;

public class LeaveCalculationEngine(IAppDbContext context) : ILeaveCalculationEngine
{
    public async Task<decimal> CalculateLeaveDaysAsync(DateTime startDate, DateTime endDate, Guid userExternalId, int tenantId, LeavePolicy usagePolicy, LeavePolicy weekOffPolicy)
    {
        var holidays = await context.Holidays
            .Where(h => h.TenantId == tenantId && h.Date >= startDate && h.Date <= endDate)
            .Select(h => DateTime.SpecifyKind(h.Date.Date, DateTimeKind.Utc))
            .ToListAsync();

        var weekOffConfig = JsonSerializer.Deserialize<WeekOffConfigDto>(weekOffPolicy.ConfigJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        var weekOffRules = weekOffConfig.Rules ?? new();

        var usageConfig = JsonSerializer.Deserialize<UsageConfigDto>(usagePolicy.ConfigJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

        decimal actualDays = 0;

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            bool isHoliday = holidays.Contains(date);
            bool isWeekOff = CheckIsWeekOff(date, weekOffRules);

            if (isHoliday)
            {
                if (usageConfig.SandwichEnabled && usageConfig.IncludeHolidaysInSandwich) actualDays += 1;
            }
            else if (isWeekOff)
            {
                if (usageConfig.SandwichEnabled && usageConfig.IncludeWeekendsInSandwich) actualDays += 1;
            }
            else
            {
                actualDays += 1;
            }
        }

        return actualDays;
    }

    private static bool CheckIsWeekOff(DateTime date, List<WeekOffRuleDto> rules)
    {
        var dayName = date.DayOfWeek.ToString();
        var rule = rules.FirstOrDefault(r => NormalizeDayName(r.Day).Equals(dayName, StringComparison.OrdinalIgnoreCase));

        if (rule == null) return false;
        if (rule.Weeks == null || rule.Weeks.Count == 0 || rule.Weeks.Contains(0)) return true;

        int weekOfMonth = (date.Day - 1) / 7 + 1;
        return rule.Weeks.Contains(weekOfMonth);
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

    private class WeekOffConfigDto
    {
        public List<WeekOffRuleDto>? Rules { get; set; }
    }

    private class UsageConfigDto
    {
        public bool SandwichEnabled { get; set; }
        public bool IncludeWeekendsInSandwich { get; set; }
        public bool IncludeHolidaysInSandwich { get; set; }
    }
}