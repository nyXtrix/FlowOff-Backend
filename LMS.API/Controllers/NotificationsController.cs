using LMS.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LMS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class NotificationsController(
    INotificationConnectionManager connectionManager,
    IAppDbContext context) : BaseController(context)
{
    [HttpGet("stream")]
    public async Task GetStream()
    {
        var userId = GetUserExternalId();
        
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        using var writer = new StreamWriter(Response.Body);
        connectionManager.AddConnection(userId, writer);

        try
        {
            await Task.Delay(Timeout.Infinite, HttpContext.RequestAborted);
        }
        catch (OperationCanceledException)
        {
            
        }
        finally
        {
            connectionManager.RemoveConnection(userId, writer);
        }
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetUserExternalId();
        var count = await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
        
        return Ok(new { count });
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetUserExternalId();
        
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(notifications);
    }

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var userId = GetUserExternalId();
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.ExternalId == id && n.UserId == userId);

        if (notification == null) return NotFound();

        notification.IsRead = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Notification marked as read" });
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetUserExternalId();
        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "All notifications marked as read" });
    }
}
