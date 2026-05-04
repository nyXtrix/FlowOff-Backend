namespace LMS.Application.Features.Leaves.Models;

public class EvaluationContext
{
    public decimal Duration { get; set; }
    public string? LeaveTypeCode { get; set; }
    public string? UserRoleName { get; set; }
    public decimal CurrentBalance { get; set; }
    public int TeamSize { get; set; }
}

public class RuleCondition
{
    public string Metric { get; set; } = null!;
    public string Operator { get; set; } = null!;
    public string Value { get; set; } = null!;
}