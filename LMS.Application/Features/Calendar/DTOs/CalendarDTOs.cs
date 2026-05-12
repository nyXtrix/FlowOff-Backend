namespace LMS.Application.Features.Calendar.DTOs;

public class CalendarResponse
{
    public List<CalendarItem> Items { get; set; } = new();
    public CalendarSummary Summary { get; set; } = new();
    public CalendarConfig Config { get; set; } = new();
}

public class CalendarItem
{
    public string Id { get; set; } = null!;
    public string Date { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string? SubType { get; set; }
    public string? Description { get; set; }
    public string? Avatar { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public class CalendarSummary
{
    public string Month { get; set; } = null!;
    public int TotalHolidays { get; set; }
    public int TotalLeaves { get; set; }
    public int TotalWeekOffs { get; set; }
    public int WorkingDays { get; set; }
}

public class CalendarConfig
{
    public List<int> DefaultWeekOffs { get; set; } = new();
    public string Timezone { get; set; } = "UTC";
}

public class HolidayCreateRequest
{
    public string Name { get; set; } = null!;
    public string Date { get; set; } = null!;
}

public class HolidayUpdateRequest
{
    public string Name { get; set; } = null!;
    public string Date { get; set; } = null!;
}
