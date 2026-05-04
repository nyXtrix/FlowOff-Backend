using System.Text.Json;
using LMS.Application.Features.Leaves.Interfaces;
using LMS.Application.Features.Leaves.Models;

namespace LMS.Application.Features.Leaves.Services;

public class RuleEvaluator : IRuleEvaluator
{
    public bool Evaluate(string ConditionJson, EvaluationContext context)
    {
        if (string.IsNullOrEmpty(ConditionJson) || ConditionJson == "[]") return true;

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var rules = JsonSerializer.Deserialize<List<RuleCondition>>(ConditionJson, options);

            if (rules == null || rules.Count == 0) return true;

            foreach (var rule in rules)
            {
                var actual = GetContextValue(rule.Metric, context);

                if (!Compare(actual, rule.Operator, rule.Value)) return false;
            }
            return true;
        }
        catch
        {
            return true;
        }
    }

    private object? GetContextValue(string metric, EvaluationContext context)
    {
        return metric.ToUpper()
        switch
        {
            "DURATION" => context.Duration,
            "LEAVE_TYPE" => context.LeaveTypeCode,
            "ROLE" => context.UserRoleName,
            "BALANCE" => context.CurrentBalance,
            "TEAM_AVAILABILITY" => context.TeamSize,
            _ => null
        };
    }

    private static bool Compare(object? actual, string operation, string target)
    {
        if (actual == null) return false;

        var sActual = actual.ToString() ?? "";

        bool IsDecimal(out decimal a, out decimal t)
        {
            var parsedA = decimal.TryParse(sActual, out a);
            var parsedT = decimal.TryParse(target, out t);
            return parsedA && parsedT;
        }

        return operation.ToUpper() switch
        {
            "EQ" => sActual.Equals(target, StringComparison.OrdinalIgnoreCase),
            "NEQ" => !sActual.Equals(target, StringComparison.OrdinalIgnoreCase),
            "GT" => IsDecimal(out var a1, out var t1) && a1 > t1,
            "LT" => IsDecimal(out var a2, out var t2) && a2 < t2,
            "GTE" => IsDecimal(out var a3, out var t3) && a3 >= t3,
            "LTE" => IsDecimal(out var a4, out var t4) && a4 <= t4,
            _ => false
        };
    }
}