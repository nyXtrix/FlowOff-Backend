namespace LMS.Domain.Entities;
using LMS.Domain.Entities.Auth;
using LMS.Domain.Entities.common;

public class UserRole : BaseEntity
{
    public int UserId { get; set; }
    public int RoleId { get; set; }

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}