using System.Linq;
using System.Text.Json;
using LMS.Application.Common.Modals;
using LMS.Application.Features.Leaves.Interfaces;
using LMS.Application.Features.Leaves.Models;

namespace LMS.Application.Features.Leaves.Services;

public class RuleEvaluator : IRuleEvaluator
{
    private static readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    public bool Evaluate(string conditionJson, EvaluationContext context)
    {
        if (string.IsNullOrEmpty(conditionJson) || conditionJson == "[]" || conditionJson == "{}") return true;

        try
        {
            var root = JsonSerializer.Deserialize<RuleGroup>(conditionJson, _options);
            if (root == null) return true;

            return EvaluateGroup(root, context);
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    private bool EvaluateGroup(RuleGroup group, EvaluationContext context)
    {
        var isAnd = group.Operator.Equals("AND", StringComparison.OrdinalIgnoreCase);
        if (group.Conditions == null || group.Conditions.Count == 0) return isAnd;

        foreach (var element in group.Conditions)
        {
            bool result;
            if (element.TryGetProperty("conditions", out _) || element.TryGetProperty("Conditions", out _))
            {
                var subGroup = JsonSerializer.Deserialize<RuleGroup>(element.GetRawText(), _options);
                result = subGroup != null && EvaluateGroup(subGroup, context);
            }
            else
            {
                var condition = JsonSerializer.Deserialize<RuleCondition>(element.GetRawText(), _options);
                result = condition != null && EvaluateCondition(condition, context);
            }

            if (isAnd && !result) return false;
            if (!isAnd && result) return true;
        }

        return isAnd;
    }

    private bool EvaluateCondition(RuleCondition condition, EvaluationContext context)
    {
        var actualValue = GetContextValue(condition.Metric, context);
        return Compare(actualValue, condition.Operator, condition.Value);
    }

    private object? GetContextValue(string metric, EvaluationContext context)
    {
        return metric.ToUpper() switch
        {
            "DURATION" => context.Duration,
            "LEAVE_TYPE" => context.LeaveTypeCode,
            "ROLE" => context.UserRoleName,
            "BALANCE" => context.CurrentBalance,
            "TEAM_AVAILABILITY" => context.TeamSize,
            _ => null
        };
    }

    private static bool Compare(object? actual, string op, object target)
    {
        if (actual == null) return false;

        var sActual = actual.ToString() ?? "";
        var sTarget = target?.ToString() ?? "";

        if (op.Equals("IN", StringComparison.OrdinalIgnoreCase) || op.Equals("NOT_IN", StringComparison.OrdinalIgnoreCase))
        {
            var isNotIn = op.Equals("NOT_IN", StringComparison.OrdinalIgnoreCase);
            if (target is JsonElement element && element.ValueKind == JsonValueKind.Array)
            {
                var items = element.EnumerateArray().Select(x => x.ToString()).ToList();
                var contains = items.Any(i => i.Equals(sActual, StringComparison.OrdinalIgnoreCase));
                return isNotIn ? !contains : contains;
            }
            return false;
        }

        if (decimal.TryParse(sActual, out var dActual) && decimal.TryParse(sTarget, out var dTarget))
        {
            return op.ToUpper() switch
            {
                "EQ" => dActual == dTarget,
                "NEQ" => dActual != dTarget,
                "GT" => dActual > dTarget,
                "LT" => dActual < dTarget,
                "GTE" => dActual >= dTarget,
                "LTE" => dActual <= dTarget,
                _ => false
            };
        }

        return op.ToUpper() switch
        {
            "EQ" => sActual.Equals(sTarget, StringComparison.OrdinalIgnoreCase),
            "NEQ" => !sActual.Equals(sTarget, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}