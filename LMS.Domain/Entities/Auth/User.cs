using LMS.Domain.Entities.common;
using LMS.Domain.Entities.Organization;
using LMS.Domain.Enums;

namespace LMS.Domain.Entities.Auth;

public class User : BaseEntity
{
    public int TenantId { get; set; }
    public string FirstName { get; set; } = null!   ;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PasswordHash { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Pending;
    public Gender Gender { get; set; } = Gender.NotSpecified;

    public string? ResetPasswordToken { get; set; }
    public DateTime? ResetTokenExpiresAt { get; set; }

    public int? ManagerId { get; set; }
    public int? DepartmentId { get; set; }
    public int RoleId { get; set; }

    public Department? Department { get; set; }

    public Role Role { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
    public User? Manager { get; set; }
    public ICollection<User>? Repotees { get; set; }
    public ICollection<UserPermissionOverride> UserPermissionOverrides { get; set; } = new List<UserPermissionOverride>();
}
