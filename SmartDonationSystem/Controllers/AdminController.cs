using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDonationSystem.Data;
using SmartDonationSystem.Models;
using SmartDonationSystem.Services;
using System.Security.Claims;

namespace SmartDonationSystem.Controllers
{
    /// <summary>
    /// Controller for Admin operations
    /// Only accessible to Admin role
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AnalyticsService _analyticsService;
        private readonly NotificationService _notificationService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            AnalyticsService analyticsService,
            NotificationService notificationService,
            ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _analyticsService = analyticsService;
            _notificationService = notificationService;
            _logger = logger;
        }

        // GET: Admin/Dashboard
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
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

            // Recent users with roles
            var recentUsers = await _userManager.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(10)
                .AsNoTracking()
                .ToListAsync();

            analytics.RecentUsers = recentUsers;
            analytics.RecentUsersWithRoles = new List<UserWithRoles>();
            foreach (var user in recentUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                analytics.RecentUsersWithRoles.Add(new UserWithRoles
                {
                    User = user,
                    Roles = roles.ToList()
                });
            }

            return View(analytics);
        }

        // GET: Admin/Users
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Users()
        {
            return await UserManagement();
        }

        /// <summary>
        /// GET: Admin/UserManagement
        /// Comprehensive user management interface
        /// </summary>
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> UserManagement(string? searchTerm = null, string? roleFilter = null)
        {
            try
            {
                var query = _userManager.Users
                    .Include(u => u.NGO)
                    .AsNoTracking();

                // Apply search filter
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(u => 
                        u.FirstName.Contains(searchTerm) ||
                        u.LastName.Contains(searchTerm) ||
                        u.Email!.Contains(searchTerm));
                }

                var users = await query.ToListAsync();

                // Filter by role if specified
                if (!string.IsNullOrEmpty(roleFilter))
                {
                    var filteredUsers = new List<ApplicationUser>();
                    foreach (var user in users)
                    {
                        var roles = await _userManager.GetRolesAsync(user);
                        if (roles.Contains(roleFilter))
                        {
                            filteredUsers.Add(user);
                        }
                    }
                    users = filteredUsers;
                }

                var userViewModels = new List<UserManagementViewModel>();
                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var donationCount = await _context.Donations.CountAsync(d => d.DonorId == user.Id);
                    var requestCount = await _context.DonationRequests
                        .Include(dr => dr.Donation)
                        .CountAsync(dr => (dr.Donation != null && dr.Donation.DonorId == user.Id) || 
                                        (dr.NGO != null && dr.NGO.UserId == user.Id));

                    userViewModels.Add(new UserManagementViewModel
                    {
                        User = user,
                        Roles = roles.ToList(),
                        DonationCount = donationCount,
                        RequestCount = requestCount,
                        IsActive = user.CreatedAt >= DateTime.UtcNow.AddDays(-30)
                    });
                }

                ViewBag.SearchTerm = searchTerm;
                ViewBag.RoleFilter = roleFilter;
                ViewBag.AvailableRoles = new[] { "Admin", "NGO", "Donor" };

                return View(userViewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user management");
                TempData["Error"] = $"An error occurred while loading users: {ex.Message}";
                return View(new List<UserManagementViewModel>());
            }
        }

        // GET: Admin/Donations
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
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
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
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

        /// <summary>
        /// GET: Admin/ApproveDonation/{id}
        /// Displays donation details for approval
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ApproveDonation(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var donation = await _context.Donations
                    .Include(d => d.Donor)
                    .Include(d => d.NGO)
                    .Include(d => d.DonationRequests)
                        .ThenInclude(dr => dr.NGO)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (donation == null)
                {
                    return NotFound();
                }

                return View(donation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading donation for approval");
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return RedirectToAction(nameof(Donations));
            }
        }

        /// <summary>
        /// POST: Admin/ApproveDonation
        /// Approves a donation and updates its status
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveDonation(int donationId, string? action = null)
        {
            try
            {
                var donation = await _context.Donations
                    .Include(d => d.Donor)
                    .Include(d => d.NGO)
                    .Include(d => d.DonationRequests)
                        .ThenInclude(dr => dr.NGO)
                    .FirstOrDefaultAsync(d => d.Id == donationId);

                if (donation == null)
                {
                    return NotFound();
                }

                if (action == "approve")
                {
                    // Approve the donation - mark as available or collected
                    if (donation.Status == "Pending" || donation.Status == "Reserved")
                    {
                        donation.Status = "Available";
                        donation.UpdatedAt = DateTime.UtcNow;

                        // Create notification for donor
                        if (donation.Donor != null)
                        {
                            var notification = new Notification
                            {
                                UserId = donation.DonorId,
                                Title = "Donation Approved",
                                Message = $"Your donation '{donation.Title}' has been approved by admin.",
                                Type = "Success",
                                RelatedEntityId = donationId,
                                RelatedEntityType = "Donation"
                            };
                            _context.Notifications.Add(notification);
                        }

                        await _context.SaveChangesAsync();
                        TempData["Success"] = "Donation approved successfully.";
                    }
                    else
                    {
                        TempData["Error"] = "Donation is not in a state that can be approved.";
                    }
                }
                else if (action == "reject")
                {
                    // Reject the donation
                    donation.Status = "Expired";
                    donation.UpdatedAt = DateTime.UtcNow;

                    // Create notification for donor
                    if (donation.Donor != null)
                    {
                        var notification = new Notification
                        {
                            UserId = donation.DonorId,
                            Title = "Donation Rejected",
                            Message = $"Your donation '{donation.Title}' has been rejected by admin.",
                            Type = "Warning",
                            RelatedEntityId = donationId,
                            RelatedEntityType = "Donation"
                        };
                        _context.Notifications.Add(notification);
                    }

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Donation rejected.";
                }
                else if (action == "mark-collected")
                {
                    // Mark as collected
                    donation.Status = "Collected";
                    donation.CollectedAt = DateTime.UtcNow;
                    donation.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Donation marked as collected.";
                }

                return RedirectToAction(nameof(Donations));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving donation");
                TempData["Error"] = $"An error occurred while processing the donation: {ex.Message}";
                return RedirectToAction(nameof(Donations));
            }
        }

        /// <summary>
        /// GET: Admin/ViewReports
        /// Displays comprehensive system reports
        /// </summary>
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ViewReports(string? reportType = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var viewModel = new AdminReportsViewModel
                {
                    ReportType = reportType ?? "overview"
                };

                // Set date range defaults
                if (!startDate.HasValue)
                {
                    startDate = DateTime.UtcNow.AddMonths(-1);
                }
                if (!endDate.HasValue)
                {
                    endDate = DateTime.UtcNow;
                }

                viewModel.StartDate = startDate.Value;
                viewModel.EndDate = endDate.Value;

                // Generate reports based on type
                switch (reportType?.ToLower())
                {
                    case "donations":
                        viewModel.DonationReport = await GenerateDonationReportAsync(startDate.Value, endDate.Value);
                        break;
                    case "users":
                        viewModel.UserReport = await GenerateUserReportAsync(startDate.Value, endDate.Value);
                        break;
                    case "ngos":
                        viewModel.NGOReport = await GenerateNGOReportAsync(startDate.Value, endDate.Value);
                        break;
                    case "analytics":
                        viewModel.AnalyticsReport = await _analyticsService.GenerateFullReportAsync();
                        break;
                    default:
                        // Overview report
                        viewModel.AnalyticsReport = await _analyticsService.GenerateFullReportAsync();
                        viewModel.DonationReport = await GenerateDonationReportAsync(startDate.Value, endDate.Value);
                        viewModel.UserReport = await GenerateUserReportAsync(startDate.Value, endDate.Value);
                        break;
                }

                ViewBag.ReportTypes = new[]
                {
                    new { Value = "overview", Text = "Overview" },
                    new { Value = "donations", Text = "Donations" },
                    new { Value = "users", Text = "Users" },
                    new { Value = "ngos", Text = "NGOs" },
                    new { Value = "analytics", Text = "Analytics" }
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating reports");
                TempData["Error"] = $"An error occurred while generating reports: {ex.Message}";
                return View(new AdminReportsViewModel());
            }
        }

        /// <summary>
        /// Generates donation report for the specified date range
        /// </summary>
        private async Task<DonationReport> GenerateDonationReportAsync(DateTime startDate, DateTime endDate)
        {
            var donations = await _context.Donations
                .Include(d => d.Donor)
                .Include(d => d.NGO)
                .Where(d => d.CreatedAt >= startDate && d.CreatedAt <= endDate)
                .AsNoTracking()
                .ToListAsync();

            return new DonationReport
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalDonations = donations.Count,
                AvailableDonations = donations.Count(d => d.Status == "Available"),
                ReservedDonations = donations.Count(d => d.Status == "Reserved"),
                CollectedDonations = donations.Count(d => d.Status == "Collected"),
                ExpiredDonations = donations.Count(d => d.Status == "Expired"),
                TotalQuantity = donations.Sum(d => d.Quantity),
                DonationsByStatus = donations.GroupBy(d => d.Status)
                    .ToDictionary(g => g.Key, g => g.Count()),
                DonationsByFoodType = donations.GroupBy(d => d.FoodType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                RecentDonations = donations.OrderByDescending(d => d.CreatedAt).Take(20).ToList()
            };
        }

        /// <summary>
        /// Generates user report for the specified date range
        /// </summary>
        private async Task<UserReport> GenerateUserReportAsync(DateTime startDate, DateTime endDate)
        {
            var users = await _userManager.Users
                .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                .AsNoTracking()
                .ToListAsync();

            var userReport = new UserReport
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalUsers = users.Count,
                NewUsers = users.Count,
                UsersByRole = new Dictionary<string, int>()
            };

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                foreach (var role in roles)
                {
                    if (!userReport.UsersByRole.ContainsKey(role))
                    {
                        userReport.UsersByRole[role] = 0;
                    }
                    userReport.UsersByRole[role]++;
                }
            }

            return userReport;
        }

        /// <summary>
        /// Generates NGO report for the specified date range
        /// </summary>
        private async Task<NGOReport> GenerateNGOReportAsync(DateTime startDate, DateTime endDate)
        {
            var ngos = await _context.NGOs
                .Include(n => n.User)
                .Include(n => n.Donations)
                .Include(n => n.DonationRequests)
                .Where(n => n.CreatedAt >= startDate && n.CreatedAt <= endDate)
                .AsNoTracking()
                .ToListAsync();

            return new NGOReport
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalNGOs = ngos.Count,
                ActiveNGOs = ngos.Count(n => n.Donations.Any() || n.DonationRequests.Any()),
                TotalDonationsReceived = ngos.Sum(n => n.Donations.Count),
                TotalRequestsMade = ngos.Sum(n => n.DonationRequests.Count),
                AverageCapacity = ngos.Any() ? (int)ngos.Average(n => n.Capacity) : 0,
                NGODetails = ngos.Select(n => new NGODetail
                {
                    Id = n.Id,
                    Name = n.Name,
                    Location = n.Location,
                    Capacity = n.Capacity,
                    DonationsReceived = n.Donations.Count,
                    RequestsMade = n.DonationRequests.Count,
                    CreatedAt = n.CreatedAt
                }).ToList()
            };
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
        public List<UserWithRoles> RecentUsersWithRoles { get; set; } = new List<UserWithRoles>();
    }

    public class UserWithRoles
    {
        public ApplicationUser User { get; set; } = new ApplicationUser();
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class UserManagementViewModel
    {
        public ApplicationUser User { get; set; } = new ApplicationUser();
        public List<string> Roles { get; set; } = new List<string>();
        public int DonationCount { get; set; }
        public int RequestCount { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// View model for admin reports
    /// </summary>
    public class AdminReportsViewModel
    {
        public string ReportType { get; set; } = "overview";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DonationReport? DonationReport { get; set; }
        public UserReport? UserReport { get; set; }
        public NGOReport? NGOReport { get; set; }
        public AnalyticsReport? AnalyticsReport { get; set; }
    }

    /// <summary>
    /// Donation report data
    /// </summary>
    public class DonationReport
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalDonations { get; set; }
        public int AvailableDonations { get; set; }
        public int ReservedDonations { get; set; }
        public int CollectedDonations { get; set; }
        public int ExpiredDonations { get; set; }
        public int TotalQuantity { get; set; }
        public Dictionary<string, int> DonationsByStatus { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> DonationsByFoodType { get; set; } = new Dictionary<string, int>();
        public List<Donation> RecentDonations { get; set; } = new List<Donation>();
    }

    /// <summary>
    /// User report data
    /// </summary>
    public class UserReport
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalUsers { get; set; }
        public int NewUsers { get; set; }
        public Dictionary<string, int> UsersByRole { get; set; } = new Dictionary<string, int>();
    }

    /// <summary>
    /// NGO report data
    /// </summary>
    public class NGOReport
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalNGOs { get; set; }
        public int ActiveNGOs { get; set; }
        public int TotalDonationsReceived { get; set; }
        public int TotalRequestsMade { get; set; }
        public int AverageCapacity { get; set; }
        public List<NGODetail> NGODetails { get; set; } = new List<NGODetail>();
    }

    /// <summary>
    /// NGO detail for reports
    /// </summary>
    public class NGODetail
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int DonationsReceived { get; set; }
        public int RequestsMade { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
