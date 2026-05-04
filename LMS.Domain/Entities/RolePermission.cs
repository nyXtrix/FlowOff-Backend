using LMS.Domain.Entities.common;
using LMS.Domain.Enums.Authorization;

namespace LMS.Domain.Entities;

public class RolePermission : BaseEntity
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }
    public ScopeType Scope { get; set; } = ScopeType.SELF;

    public Role Role { get; set; } = null!;
    public Permissions Permissions { get; set; } = null!;
}