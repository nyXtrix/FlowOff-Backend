namespace LMS.Application.Features.Dashboard.DTOs;

public class UserDashboardResponse
{
    public List<StatCardDto> StatCards { get; set; } = new();
    public List<LeaveBalanceItem> LeaveBalances { get; set; } = new();
    public List<RecentActivityItem> RecentActivities { get; set; } = new();
}

public class StatCardDto
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public object Value { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
}

public class LeaveBalanceItem
{
    public string LeaveType { get; set; } = string.Empty;
    public decimal Consumed { get; set; }
    public decimal Total { get; set; }
}

public class RecentActivityItem
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
