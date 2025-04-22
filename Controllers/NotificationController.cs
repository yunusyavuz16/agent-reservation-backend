using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservationApi.Data;
using ReservationApi.DTOs;
using ReservationApi.Models;

namespace ReservationApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(ApplicationDbContext context, ILogger<NotificationController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/Notification
    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetNotifications()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                ReservationId = n.ReservationId,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return notifications;
    }

    // GET: api/Notification/unread
    [HttpGet("unread")]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetUnreadNotifications()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                ReservationId = n.ReservationId,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return notifications;
    }

    // GET: api/Notification/5
    [HttpGet("{id}")]
    public async Task<ActionResult<NotificationDto>> GetNotification(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id);

        if (notification == null)
        {
            return NotFound();
        }

        // Check if notification belongs to user or user is admin
        if (notification.UserId != userId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        return new NotificationDto
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            ReservationId = notification.ReservationId,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt
        };
    }

    // POST: api/Notification
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<NotificationDto>> CreateNotification(CreateNotificationDto notificationDto)
    {
        var notification = new Notification
        {
            UserId = notificationDto.UserId,
            Title = notificationDto.Title,
            Message = notificationDto.Message,
            Type = notificationDto.Type,
            ReservationId = notificationDto.ReservationId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetNotification),
            new { id = notification.Id },
            new NotificationDto
            {
                Id = notification.Id,
                UserId = notification.UserId,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                ReservationId = notification.ReservationId,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt
            });
    }

    // PUT: api/Notification/5/read
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var notification = await _context.Notifications.FindAsync(id);
        if (notification == null)
        {
            return NotFound();
        }

        // Check if notification belongs to user or user is admin
        if (notification.UserId != userId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        // Mark as read
        notification.IsRead = true;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!NotificationExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // PUT: api/Notification/markAllRead
    [HttpPut("markAllRead")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Notification/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var notification = await _context.Notifications.FindAsync(id);
        if (notification == null)
        {
            return NotFound();
        }

        // Check if notification belongs to user or user is admin
        if (notification.UserId != userId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool NotificationExists(int id)
    {
        return _context.Notifications.Any(e => e.Id == id);
    }
}