using LMS.Domain.Entities.common;

namespace LMS.Domain.Entities;

public class Permissions : BaseEntity
{
    public string Name { get; set; } = null!;

    public ICollection<RolePermission> RolePermissions { get; set; } = null!;
}