namespace LMS.Domain.Entities;
using LMS.Domain.Entities.Auth;
using LMS.Domain.Entities.common;

public class Position : BaseEntity
{
    public int TenantId { get; set; }

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public string Name { get; set; } = null!;

    public ICollection<User> Users { get; set; } = null!;

}