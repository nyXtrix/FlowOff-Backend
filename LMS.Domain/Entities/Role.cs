using LMS.Domain.Entities.common;
using LMS.Domain.Entities.Organization;
using LMS.Domain.Enums.Authorization;

namespace LMS.Domain.Entities;

public class Role : BaseEntity
{
    public int TenantId { get; set; }

    public string Name { get; set; } = null!;

    public ScopeType Scope { get; set; } = ScopeType.SELF;

    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = null!;
    public ICollection<UserRole> UserRoles { get; set; } = null!;
}
