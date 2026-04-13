using FluentValidation;
using LMS.Application.Common.Modals;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LMS.API.Filters;

public class ValidationFilter(IServiceProvider serviceProvider) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument == null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            var validator = serviceProvider.GetService(validatorType) as IValidator;
            if (validator == null) continue;

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext);

            if (!result.IsValid)
            {
                var fieldErrors = result.Errors
                    .GroupBy(e => e.PropertyName)
                    .Select(group => new ValidationErrorResponse
                    {
                        ErrorCode = "VALIDATION_FAILED",
                        Field = group.Key,
                        Message = group.Count() == 1
                            ? group.First().ErrorMessage
                            : group.Select(e => e.ErrorMessage).ToList()
                    })
                    .ToList();

                context.Result = new BadRequestObjectResult(fieldErrors);
                return;
            }
        }

        await next();
    }
}
