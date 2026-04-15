using LMS.Domain.Entities.common;
using LMS.Domain.Entities.Auth;

namespace LMS.Domain.Entities.Organization;

public class Department : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public ICollection<Role> Roles { get; set; } = new List<Role>();
    public ICollection<User> Employees { get; set; } = new List<User>();
}
