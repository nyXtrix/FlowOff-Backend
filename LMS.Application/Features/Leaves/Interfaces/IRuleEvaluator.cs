using LMS.Application.Features.Leaves.Models;

namespace LMS.Application.Features.Leaves.Interfaces;

public interface IRuleEvaluator
{
    bool Evaluate(string ConditionJson, EvaluationContext context);
}