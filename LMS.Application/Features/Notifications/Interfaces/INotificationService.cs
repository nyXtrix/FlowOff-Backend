namespace LMS.Application.Features.Notifications.Interfaces;

public interface INotificationService
{
    Task SendNotificationAsync(Guid userId, string title, string message, string type, int tenantId, string? targetUrl = null);
}
