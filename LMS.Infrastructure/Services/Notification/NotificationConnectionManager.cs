using System.Collections.Concurrent;
using System.Text.Json;

using LMS.Application.Common.Interfaces;

namespace LMS.Infrastructure.Services.Notifications;

public class NotificationConnectionManager : INotificationConnectionManager
{
    private readonly ConcurrentDictionary<Guid, List<StreamWriter>> _userConnections = new();

    public void AddConnection(Guid userId, StreamWriter writer)
    {
        var connections = _userConnections.GetOrAdd(userId, _ => new List<StreamWriter>());
        lock (connections) { connections.Add(writer); }
    }

    public void RemoveConnection(Guid userId, StreamWriter writer)
    {
        if (_userConnections.TryGetValue(userId, out var connections))
        {
            lock (connections) { connections.Remove(writer); }
        }
    }

    public async Task BroadcastToUserAsync(Guid userId, object notification)
    {
        if (_userConnections.TryGetValue(userId, out var connections))
        {
            var data = $"data: {JsonSerializer.Serialize(notification)}\n\n";
            
            List<StreamWriter> activeConnections;
            lock (connections)
            {
                activeConnections = connections.ToList();
            }

            foreach (var writer in activeConnections)
            {
                try
                {
                    await writer.WriteAsync(data);
                    await writer.FlushAsync();
                }
                catch
                {
                    RemoveConnection(userId, writer);
                }
            }
        }
    }

    public async Task SendHeartbeatAsync()
    {
        var ping = ":ping\n\n";
        foreach (var userId in _userConnections.Keys)
        {
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                List<StreamWriter> activeConnections;
                lock (connections)
                {
                    activeConnections = connections.ToList();
                }

                foreach (var writer in activeConnections)
                {
                    try
                    {
                        await writer.WriteAsync(ping);
                        await writer.FlushAsync();
                    }
                    catch
                    {
                        
                    }
                }
            }
        }
    }
}