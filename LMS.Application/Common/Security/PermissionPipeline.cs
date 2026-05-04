using LMS.Domain.Entities.Auth;
using LMS.Domain.Module.Authorization;

namespace LMS.Application.Common.Security;

public class PermissionContext(User user)
{
    public User User { get; } = user;
    public AppPermissions Permissions { get; } = new();
}

public interface IPermissionStrategy
{
    int Priority { get; }
    Task ExecuteAsync(PermissionContext context);
}
