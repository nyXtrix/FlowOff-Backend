namespace LMS.Application.Common.Interfaces;

public interface INotificationConnectionManager
{
    void AddConnection(Guid userId, StreamWriter writer);
    void RemoveConnection(Guid userId, StreamWriter writer);
    Task BroadcastToUserAsync(Guid userId, object notification);
    Task SendHeartbeatAsync();
}
