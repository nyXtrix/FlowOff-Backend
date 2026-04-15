using LMS.Application.Common.Interfaces;
using LMS.Domain.Enums.Authorization;
using LMS.Domain.Module.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LMS.API.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class AuthorizePermissionAttribute(string module, ActionType action) : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var authService = context.HttpContext.RequestServices.GetRequiredService<ILmsAuthorizationService>();

        var userPermissions = context.HttpContext.Items["UserPermissions"] as AppPermissions;

        if (!authService.CanPerform(userPermissions, module, action))
        {
            context.Result = new ForbidResult();
        }
    }
}