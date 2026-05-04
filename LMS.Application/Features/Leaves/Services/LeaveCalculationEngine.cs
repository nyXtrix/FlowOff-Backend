using System.Text.Json;
using LMS.Application.Common.Interfaces;
using LMS.Application.Features.Leaves.DTOs.Admin;
using LMS.Application.Features.Leaves.Interfaces;
using LMS.Domain.Entities.Leave;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Leaves.Services;

public class LeaveCalculationEngine(IAppDbContext context) : ILeaveCalculationEngine
{
    public async Task<decimal> CalculateLeaveDaysAsync(DateTime startDate, DateTime endDate, Guid userExternalId, int tenantId, LeaveUsagePolicy usagePolicy, WeekOffPolicy weekOffPolicy)
    {
        var holidays = await context.Holidays.Where(h => h.TenantId == tenantId && h.Date >= startDate && h.Date <= endDate).Select(h => h.Date.Date).ToListAsync();
        var rulesJson = string.IsNullOrWhiteSpace(weekOffPolicy.RulesJson) ? "[]" : weekOffPolicy.RulesJson;
        var weekOffRules = JsonSerializer.Deserialize<List<WeekOffRuleDto>>(rulesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

        decimal actualDays = 0;

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            bool isHoliday = holidays.Contains(date);
            bool isWeekOff = CheckIsWeekOff(date, weekOffRules);

            if (isHoliday)
            {
                if(usagePolicy.SandwichEnabled && usagePolicy.IncludeHolidaysInSandwich) actualDays += 1;
            }
            else if (isWeekOff)
            {
                if(usagePolicy.SandwichEnabled && usagePolicy.IncludeWeekendsInSandwich) actualDays += 1;
            }
            else
            {
                actualDays += 1;
            }
        }

        return actualDays;
    }

    private bool CheckIsWeekOff(DateTime date, List<WeekOffRuleDto> rules)
    {
        var dayName = date.DayOfWeek.ToString();
        var rule = rules.FirstOrDefault(r => r.Day.Equals(dayName, StringComparison.OrdinalIgnoreCase));

        if (rule == null) return false;

        if (rule.Weeks.Contains(0)) return true;

        int weekOfMonth = (date.Day - 1) / 7 + 1;

        return rule.Weeks.Contains(weekOfMonth);
    }
}