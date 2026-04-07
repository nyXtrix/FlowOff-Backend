using LMS.Domain.Entities.common;
using LMS.Domain.Enums;

namespace LMS.Domain.Entities.Auth;

public class TenantLead : BaseEntity
{
    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Message { get; set; }

    public InquiryPurpose Purpose { get; set; }

    public string RegistrationToken { get; set; } = Guid.NewGuid().ToString();

    public DateTime TokenExpiresAt { get; set; } = DateTime.UtcNow.AddDays(1);
}