using LMS.Domain.Entities.Auth;
using LMS.Domain.Module.Authorization;

namespace LMS.Application.Common.Security;

public interface IPermissionResolver
{
    Task<AppPermissions> ResolveForUserAsync(User user);
}

public class PermissionResolver(IEnumerable<IPermissionStrategy> strategies) : IPermissionResolver
{
    public async Task<AppPermissions> ResolveForUserAsync(User user)
    {
        var context = new PermissionContext(user);
        
        foreach (var strategy in strategies.OrderBy(s => s.Priority))
        {
            await strategy.ExecuteAsync(context);
        }

        return context.Permissions;
    }
}
