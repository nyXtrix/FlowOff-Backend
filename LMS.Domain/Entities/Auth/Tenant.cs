
using LMS.Domain.Entities.common;

namespace LMS.Domain.Entities.Auth;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Domain { get; set; } = string.Empty;

    public string WeekoffDays { get; set; } = "0,6";

    public ICollection<User> Users { get; set; } = null!;

    public int DailyInviteLimit { get; set; } = 200;

    public DateTime LastQuotaResetDate { get; set; } = DateTime.UtcNow;

    public int RemainingDailyInvites { get; set; } = 200;

}