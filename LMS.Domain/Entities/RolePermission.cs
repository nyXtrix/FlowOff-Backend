using LMS.Domain.Entities.common;

namespace LMS.Domain.Entities;

public class RolePermission : BaseEntity
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }

    public Role Role { get; set; } = null!;
    public Permissions Permissions { get; set; } = null!;
}