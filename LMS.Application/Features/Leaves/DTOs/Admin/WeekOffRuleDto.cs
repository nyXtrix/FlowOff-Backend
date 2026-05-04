namespace LMS.Application.Features.Leaves.DTOs.Admin;

public class WeekOffRuleDto
{
    public string Day { get; set; } = string.Empty;

    public List<int> Weeks { get; set; } = new();
}