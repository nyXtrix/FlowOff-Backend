using System.Text.Json;
namespace LMS.Application.Features.Leaves.Models;

public class EvaluationContext
{
    public decimal Duration { get; set; }
    public string? LeaveTypeCode { get; set; }
    public string? UserRoleName { get; set; }
    public decimal CurrentBalance { get; set; }
    public int TeamSize { get; set; }
}

public class RuleGroup
{
    public string Operator { get; set; } = "AND";
    public List<JsonElement> Conditions { get; set; } = new();
}

public class RuleCondition
{
    public string Metric { get; set; } = null!;
    public string Operator { get; set; } = null!;
    public object Value { get; set; } = null!;
}