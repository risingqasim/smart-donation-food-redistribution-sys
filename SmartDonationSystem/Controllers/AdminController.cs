using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDonationSystem.Data;
using SmartDonationSystem.Models;
using System.Security.Claims;

namespace SmartDonationSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var analytics = new AdminAnalyticsViewModel
            {
                TotalUsers = await _userManager.Users.CountAsync(),
                TotalDonations = await _context.Donations.CountAsync(),
                TotalNGOs = await _context.NGOs.CountAsync(),
                AvailableDonations = await _context.Donations.CountAsync(d => d.Status == "Available"),
                CollectedDonations = await _context.Donations.CountAsync(d => d.Status == "Collected"),
                TotalDonationRequests = await _context.DonationRequests.CountAsync(),
                PendingRequests = await _context.DonationRequests.CountAsync(dr => dr.Status == "Pending"),
                ApprovedRequests = await _context.DonationRequests.CountAsync(dr => dr.Status == "Approved")
            };

            // Recent donations
            analytics.RecentDonations = await _context.Donations
                .Include(d => d.Donor)
                .Include(d => d.NGO)
                .OrderByDescending(d => d.CreatedAt)
                .Take(10)
                .AsNoTracking()
                .ToListAsync();

            // Recent users
            analytics.RecentUsers = await _userManager.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(10)
                .AsNoTracking()
                .ToListAsync();

            return View(analytics);
        }

        // GET: Admin/Users
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users
                .Include(u => u.NGO)
                .AsNoTracking()
                .ToListAsync();

            var userViewModels = new List<UserManagementViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new UserManagementViewModel
                {
                    User = user,
                    Roles = roles.ToList(),
                    DonationCount = await _context.Donations.CountAsync(d => d.DonorId == user.Id)
                });
            }

            return View(userViewModels);
        }

        // GET: Admin/Donations
        public async Task<IActionResult> Donations()
        {
            var donations = await _context.Donations
                .Include(d => d.Donor)
                .Include(d => d.NGO)
                .AsNoTracking()
                .ToListAsync();

            return View(donations);
        }

        // GET: Admin/NGOManagement
        public async Task<IActionResult> NGOManagement()
        {
            var ngos = await _context.NGOs
                .Include(n => n.User)
                .Include(n => n.Donations)
                .Include(n => n.DonationRequests)
                .AsNoTracking()
                .ToListAsync();

            return View(ngos);
        }

        // POST: Admin/DeleteUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Check if user has donations
            var hasDonations = await _context.Donations.AnyAsync(d => d.DonorId == userId);
            if (hasDonations)
            {
                TempData["Error"] = "Cannot delete user with existing donations.";
                return RedirectToAction(nameof(Users));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "User deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to delete user.";
            }

            return RedirectToAction(nameof(Users));
        }

        // POST: Admin/ChangeUserRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUserRole(string userId, string newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, newRole);

            TempData["Success"] = $"User role changed to {newRole}.";
            return RedirectToAction(nameof(Users));
        }

        // POST: Admin/DeleteDonation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDonation(int id)
        {
            var donation = await _context.Donations.FindAsync(id);
            if (donation == null)
            {
                return NotFound();
            }

            _context.Donations.Remove(donation);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Donation deleted successfully.";
            return RedirectToAction(nameof(Donations));
        }
    }

    public class AdminAnalyticsViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalDonations { get; set; }
        public int TotalNGOs { get; set; }
        public int AvailableDonations { get; set; }
        public int CollectedDonations { get; set; }
        public int TotalDonationRequests { get; set; }
        public int PendingRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public List<Donation> RecentDonations { get; set; } = new List<Donation>();
        public List<ApplicationUser> RecentUsers { get; set; } = new List<ApplicationUser>();
    }

    public class UserManagementViewModel
    {
        public ApplicationUser User { get; set; } = new ApplicationUser();
        public List<string> Roles { get; set; } = new List<string>();
        public int DonationCount { get; set; }
    }
}
