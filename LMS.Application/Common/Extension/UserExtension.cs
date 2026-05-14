using LMS.Application.Common.Modals;
using LMS.Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Common.Extension;

public static class UserExtensions
{
    public static async Task<User> GetUserByExternalIdAsync(this IQueryable<User> users, Guid ecternalId)
    {
        return await users.FirstOrDefaultAsync(u => u.ExternalId == ecternalId) ?? throw new AppException(404, "User account not found in this system", "NOT_FOUND");
    }

    public static IQueryable<User> WithPermissions(this IQueryable<User> query)
    {
        return query.Include(u => u.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permissions)
                .Include(u => u.UserPermissionOverrides)
                    .ThenInclude(upo => upo.Permissions);
    }
}