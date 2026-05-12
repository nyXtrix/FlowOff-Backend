using LMS.Application.Common.Interfaces;
using LMS.Application.Features.Notifications.Interfaces;
using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace LMS.Application.Features.Notifications.Services;

public class NotificationService(
    IAppDbContext context,
    INotificationConnectionManager connectionManager) : INotificationService
{
    public async Task SendNotificationAsync(Guid userId, string title, string message, string type, int tenantId, string? targetUrl = null)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.ExternalId == userId)
            ?? throw new Exception("User not found");

        var notification = new Notification
        {
            UserId = user.ExternalId,
            TenantId = tenantId,
            Title = title,
            Message = message,
            Type = type,
            TargetUrl = targetUrl,
            IsRead = false
        };

        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        await connectionManager.BroadcastToUserAsync(userId, new
        {
            externalId = notification.ExternalId,
            title = notification.Title,
            message = notification.Message,
            type = notification.Type,
            targetUrl = notification.TargetUrl,
            createdAt = notification.CreatedAt
        });
    }
}
