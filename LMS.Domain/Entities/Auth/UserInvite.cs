using LMS.Domain.Entities.common;

namespace LMS.Domain.Entities.Auth;

public class UserInvite : BaseEntity
{
    public int UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public bool IsUsed { get; set; } = false;

    public User User { get; set; } = null!;
}
