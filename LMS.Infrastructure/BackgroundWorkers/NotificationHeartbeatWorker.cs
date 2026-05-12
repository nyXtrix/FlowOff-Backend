using LMS.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.BackgroundWorkers;

public class NotificationHeartbeatWorker(
    INotificationConnectionManager connectionManager,
    ILogger<NotificationHeartbeatWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Notification Heartbeat Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await connectionManager.SendHeartbeatAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while sending notification heartbeats.");
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }

        logger.LogInformation("Notification Heartbeat Worker stopped.");
    }
}
