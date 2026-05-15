using System.Security.Claims;
using LMS.Application.Common.Interfaces;
using LMS.Domain.Module.Authorization;
using LMS.Domain.Enums.Authorization;
using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using LMS.Application.Features.Auth.Interfaces;

namespace LMS.API.Middleware;

public class UserPermissionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAuthenticationService authService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var claim = context.User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null && Guid.TryParse(claim.Value, out var userId))
            {
                try
                {
                    var userProfile = await authService.GetCurrentUserAsync(userId);
                    context.Items["UserPermissions"] = userProfile.Permissions;
                }
                catch
                {}
            }
        }
        await next(context);
    }
}