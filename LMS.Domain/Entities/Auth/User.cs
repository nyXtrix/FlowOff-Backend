using LMS.Domain.Entities.common;
using LMS.Domain.Enums;

namespace LMS.Domain.Entities.Auth;

public class User : BaseEntity
{
    public int TenantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Pending;
    public Gender Gender { get; set; } = Gender.NotSpecified;
    
    public string? ResetPasswordToken { get; set; }
    public DateTime? ResetTokenExpiresAt { get; set; }

    public int? ManagerId { get; set; }
    public int RoleId { get; set; }
    public int PositionId { get; set; }
    
    public Role Role { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;

    public Position Position { get; set; } = null!;
    public User? Manager { get; set; }
    public ICollection<UserPermissionOverride> UserPermissionOverrides { get; set; } = null!;
    public ICollection<User>? Repotees { get; set; }
}