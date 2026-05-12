using LMS.Domain.Entities.common;

namespace LMS.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public int TenantId { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string Type { get; set; } = "Info";
    public string? TargetUrl { get; set; }
    public bool IsRead { get; set; } = false;
    
}