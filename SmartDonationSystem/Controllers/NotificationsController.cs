using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDonationSystem.Data;
using SmartDonationSystem.Models;
using System.Security.Claims;

namespace SmartDonationSystem.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Notifications
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.Timestamp)
                .AsNoTracking()
                .ToListAsync();

            return View(notifications);
        }

        // GET: Notifications/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null)
            {
                return NotFound();
            }

            // Mark as read
            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return View(notification);
        }

        // POST: Notifications/MarkAsRead/5
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        // POST: Notifications/MarkAllAsRead
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // DELETE: Notifications/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        // GET: Notifications/Count
        [HttpGet]
        public async Task<IActionResult> Count()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var count = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            return Json(count);
        }
    }
}
