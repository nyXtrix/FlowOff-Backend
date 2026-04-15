using LMS.Application.Common.Interfaces;
using LMS.Domain.Module.Authorization;
using LMS.Domain.Enums.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace LMS.Application.Features.Auth.Services.Authorization;

public class AuthorizationService(IHttpContextAccessor httpContextAccessor) : ILmsAuthorizationService
{
    public bool CanPerform(AppPermissions permissions, string module, ActionType action, ResourceContext? resource = null)
    {
        if (permissions == null || !permissions.TryGetValue(module, out var permission)) return false;

        if (!permission.Actions.Contains(action)) return false;

        if (resource == null) return true;

        var currentUserId = GetCurrentUserId();

        if (module == "APPROVALS" && (action == ActionType.APPROVE || action == ActionType.REJECT))
        {
            if (resource.UserId == currentUserId) return false;
        }

        return permission.Scope switch
        {
            ScopeType.ALL => true,
            ScopeType.TEAM => resource.ManagerId == currentUserId,
            ScopeType.SELF => resource.UserId == currentUserId,
            _ => false
        };
    }


    private Guid GetCurrentUserId()
    {
        var claim = httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }
}
