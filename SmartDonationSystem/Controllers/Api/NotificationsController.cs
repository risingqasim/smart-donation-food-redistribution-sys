using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDonationSystem.Data;
using SmartDonationSystem.Models;
using System.Security.Claims;

namespace SmartDonationSystem.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/notifications
        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] bool? isRead = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var query = _context.Notifications
                .Where(n => n.UserId == userId)
                .AsNoTracking();

            if (isRead.HasValue)
            {
                query = query.Where(n => n.IsRead == isRead.Value);
            }

            var totalCount = await query.CountAsync();
            var notifications = await query
                .OrderByDescending(n => n.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                Notifications = notifications,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }

        // GET: api/notifications/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetNotification(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var notification = await _context.Notifications
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null)
            {
                return NotFound();
            }

            return Ok(notification);
        }

        // PUT: api/notifications/{id}/read
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null)
            {
                return NotFound();
            }

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(notification);
        }

        // PUT: api/notifications/{id}/unread
        [HttpPut("{id}/unread")]
        public async Task<IActionResult> MarkAsUnread(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null)
            {
                return NotFound();
            }

            notification.IsRead = false;
            notification.ReadAt = null;

            await _context.SaveChangesAsync();

            return Ok(notification);
        }

        // PUT: api/notifications/mark-all-read
        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "All notifications marked as read.", Count = unreadNotifications.Count });
        }

        // DELETE: api/notifications/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null)
            {
                return NotFound();
            }

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/notifications/clear-all
        [HttpDelete("clear-all")]
        public async Task<IActionResult> ClearAllNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var userNotifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .ToListAsync();

            _context.Notifications.RemoveRange(userNotifications);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "All notifications cleared.", Count = userNotifications.Count });
        }

        // GET: api/notifications/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetNotificationStats()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var stats = new
            {
                Total = await _context.Notifications.CountAsync(n => n.UserId == userId),
                Unread = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead),
                Read = await _context.Notifications.CountAsync(n => n.UserId == userId && n.IsRead),
                Today = await _context.Notifications.CountAsync(n => n.UserId == userId && n.Timestamp.Date == DateTime.Today)
            };

            return Ok(stats);
        }
    }
}
