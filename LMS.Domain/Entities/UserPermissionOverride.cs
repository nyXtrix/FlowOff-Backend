namespace LMS.Domain.Entities;
using LMS.Domain.Entities.Auth;
using LMS.Domain.Entities.common;

public class UserPermissionOverride : BaseEntity
{
    public int UserId { get; set; }
    public int PermissionId { get; set; }

    public bool IsAllowed { get; set; }

    public User User { get; set; } = null!;
    public Permissions Permissions { get; set; } = null!;
}