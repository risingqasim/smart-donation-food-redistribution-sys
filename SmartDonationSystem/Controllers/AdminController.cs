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

            // Chart data for dashboard - limit to 6 months for summary
            var monthlyTrends = await _analyticsService.GetMonthlyTrendsAsync(6); // Last 6 months
            var foodTypeData = await _analyticsService.GetFoodTypeDistributionAsync();
            
            ViewBag.MonthlyTrends = monthlyTrends;
            ViewBag.FoodTypeData = foodTypeData;

            return View(analytics);
        }

        // GET: Admin/MonthlyReports
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> MonthlyReports(int? months = null)
        {
            try
            {
                var monthsToShow = months ?? 12; // Default to 12 months for detailed view
                var monthlyTrends = await _analyticsService.GetMonthlyTrendsAsync(monthsToShow);
                
                // Get detailed donations grouped by month
                var startDate = DateTime.UtcNow.AddMonths(-monthsToShow);
                var donations = await _context.Donations
                    .Include(d => d.Donor)
                    .Include(d => d.NGO)
                    .Where(d => d.CreatedAt >= startDate)
                    .AsNoTracking()
                    .OrderByDescending(d => d.CreatedAt)
                    .ToListAsync();

                var monthlyDonations = new Dictionary<string, List<Donation>>();
                foreach (var donation in donations)
                {
                    var monthKey = donation.CreatedAt.ToString("MMM yyyy");
                    if (!monthlyDonations.ContainsKey(monthKey))
                    {
                        monthlyDonations[monthKey] = new List<Donation>();
                    }
                    monthlyDonations[monthKey].Add(donation);
                }

                ViewBag.MonthlyTrends = monthlyTrends;
                ViewBag.MonthlyDonations = monthlyDonations;
                ViewBag.MonthsToShow = monthsToShow;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading monthly reports");
                TempData["Error"] = $"An error occurred while loading monthly reports: {ex.Message}";
                return View();
            }
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

                // Set date range defaults with validation
                if (!startDate.HasValue)
                {
                    startDate = DateTime.UtcNow.AddMonths(-1);
                }
                if (!endDate.HasValue)
                {
                    endDate = DateTime.UtcNow;
                }

                // Validate date range
                if (startDate.Value > endDate.Value)
                {
                    TempData["Error"] = "Start date cannot be after end date.";
                    startDate = DateTime.UtcNow.AddMonths(-1);
                    endDate = DateTime.UtcNow;
                }

                viewModel.StartDate = startDate.Value;
                viewModel.EndDate = endDate.Value;

                // Generate reports based on type with individual error handling
                switch (reportType?.ToLower())
                {
                    case "donations":
                        try
                        {
                            viewModel.DonationReport = await GenerateDonationReportAsync(startDate.Value, endDate.Value);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error generating donation report");
                            viewModel.DonationReport = new DonationReport
                            {
                                StartDate = startDate.Value,
                                EndDate = endDate.Value
                            };
                        }
                        break;
                    case "users":
                        try
                        {
                            viewModel.UserReport = await GenerateUserReportAsync(startDate.Value, endDate.Value);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error generating user report");
                            viewModel.UserReport = new UserReport
                            {
                                StartDate = startDate.Value,
                                EndDate = endDate.Value
                            };
                        }
                        break;
                    case "ngos":
                        try
                        {
                            viewModel.NGOReport = await GenerateNGOReportAsync(startDate.Value, endDate.Value);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error generating NGO report");
                            viewModel.NGOReport = new NGOReport
                            {
                                StartDate = startDate.Value,
                                EndDate = endDate.Value
                            };
                        }
                        break;
                    case "analytics":
                        try
                        {
                            viewModel.AnalyticsReport = await _analyticsService.GenerateFullReportAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error generating analytics report");
                            viewModel.AnalyticsReport = new AnalyticsReport();
                        }
                        break;
                    default:
                        // Overview report - generate all with individual error handling
                        try
                        {
                            viewModel.AnalyticsReport = await _analyticsService.GenerateFullReportAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error generating analytics report");
                            viewModel.AnalyticsReport = new AnalyticsReport();
                        }
                        try
                        {
                            viewModel.DonationReport = await GenerateDonationReportAsync(startDate.Value, endDate.Value);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error generating donation report");
                            viewModel.DonationReport = new DonationReport
                            {
                                StartDate = startDate.Value,
                                EndDate = endDate.Value
                            };
                        }
                        try
                        {
                            viewModel.UserReport = await GenerateUserReportAsync(startDate.Value, endDate.Value);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error generating user report");
                            viewModel.UserReport = new UserReport
                            {
                                StartDate = startDate.Value,
                                EndDate = endDate.Value
                            };
                        }
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
                TempData["Error"] = "An error occurred while generating reports. Please try again or contact support if the problem persists.";
                return View(new AdminReportsViewModel
                {
                    ReportType = reportType ?? "overview",
                    StartDate = startDate ?? DateTime.UtcNow.AddMonths(-1),
                    EndDate = endDate ?? DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Generates donation report for the specified date range
        /// </summary>
        private async Task<DonationReport> GenerateDonationReportAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var donations = await _context.Donations
                    .Include(d => d.Donor)
                    .Include(d => d.NGO)
                    .Where(d => d.CreatedAt != null && d.CreatedAt >= startDate && d.CreatedAt <= endDate)
                    .AsNoTracking()
                    .ToListAsync();

                // Null-safe grouping for Status
                var donationsByStatus = donations
                    .Where(d => !string.IsNullOrEmpty(d.Status))
                    .GroupBy(d => d.Status ?? "Unknown")
                    .ToDictionary(g => g.Key ?? "Unknown", g => g.Count());

                // Null-safe grouping for FoodType
                var donationsByFoodType = donations
                    .Where(d => !string.IsNullOrEmpty(d.FoodType))
                    .GroupBy(d => d.FoodType ?? "Unknown")
                    .ToDictionary(g => g.Key ?? "Unknown", g => g.Count());

                // Null-safe quantity sum
                var totalQuantity = donations
                    .Where(d => d.Quantity > 0)
                    .Sum(d => (double?)d.Quantity) ?? 0;

                // Null-safe date ordering
                var recentDonations = donations
                    .Where(d => d.CreatedAt != null)
                    .OrderByDescending(d => d.CreatedAt)
                    .Take(20)
                    .ToList();

                return new DonationReport
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalDonations = donations.Count,
                    AvailableDonations = donations.Count(d => d.Status == "Available"),
                    ReservedDonations = donations.Count(d => d.Status == "Reserved"),
                    CollectedDonations = donations.Count(d => d.Status == "Collected"),
                    ExpiredDonations = donations.Count(d => d.Status == "Expired"),
                    TotalQuantity = (int)totalQuantity,
                    DonationsByStatus = donationsByStatus,
                    DonationsByFoodType = donationsByFoodType,
                    RecentDonations = recentDonations
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating donation report for {StartDate} to {EndDate}", startDate, endDate);
                // Return empty report instead of throwing
                return new DonationReport
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalDonations = 0,
                    AvailableDonations = 0,
                    ReservedDonations = 0,
                    CollectedDonations = 0,
                    ExpiredDonations = 0,
                    TotalQuantity = 0,
                    DonationsByStatus = new Dictionary<string, int>(),
                    DonationsByFoodType = new Dictionary<string, int>(),
                    RecentDonations = new List<Donation>()
                };
            }
        }

        /// <summary>
        /// Generates user report for the specified date range
        /// </summary>
        private async Task<UserReport> GenerateUserReportAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var users = await _userManager.Users
                    .Where(u => u.CreatedAt != null && u.CreatedAt >= startDate && u.CreatedAt <= endDate)
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
                    try
                    {
                        var roles = await _userManager.GetRolesAsync(user);
                        foreach (var role in roles ?? Enumerable.Empty<string>())
                        {
                            if (!string.IsNullOrEmpty(role))
                            {
                                if (!userReport.UsersByRole.ContainsKey(role))
                                {
                                    userReport.UsersByRole[role] = 0;
                                }
                                userReport.UsersByRole[role]++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error getting roles for user {UserId}", user?.Id);
                        // Continue with next user
                    }
                }

                return userReport;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating user report for {StartDate} to {EndDate}", startDate, endDate);
                return new UserReport
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalUsers = 0,
                    NewUsers = 0,
                    UsersByRole = new Dictionary<string, int>()
                };
            }
        }

        /// <summary>
        /// Generates NGO report for the specified date range
        /// </summary>
        private async Task<NGOReport> GenerateNGOReportAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var ngos = await _context.NGOs
                    .Include(n => n.User)
                    .Include(n => n.Donations)
                    .Include(n => n.DonationRequests)
                    .Where(n => n.CreatedAt != null && n.CreatedAt >= startDate && n.CreatedAt <= endDate)
                    .AsNoTracking()
                    .ToListAsync();

                // Null-safe calculations
                var activeNGOs = ngos.Count(n => 
                    (n.Donations != null && n.Donations.Any()) || 
                    (n.DonationRequests != null && n.DonationRequests.Any()));

                var totalDonationsReceived = ngos
                    .Where(n => n.Donations != null)
                    .Sum(n => n.Donations.Count);

                var totalRequestsMade = ngos
                    .Where(n => n.DonationRequests != null)
                    .Sum(n => n.DonationRequests.Count);

                var averageCapacity = ngos.Any() && ngos.All(n => n.Capacity > 0)
                    ? (int)ngos.Average(n => n.Capacity)
                    : 0;

                var ngoDetails = ngos.Select(n => new NGODetail
                {
                    Id = n.Id,
                    Name = n.Name ?? "Unknown",
                    Location = n.Location ?? "Unknown",
                    Capacity = n.Capacity,
                    DonationsReceived = n.Donations?.Count ?? 0,
                    RequestsMade = n.DonationRequests?.Count ?? 0,
                    CreatedAt = n.CreatedAt
                }).ToList();

                return new NGOReport
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalNGOs = ngos.Count,
                    ActiveNGOs = activeNGOs,
                    TotalDonationsReceived = totalDonationsReceived,
                    TotalRequestsMade = totalRequestsMade,
                    AverageCapacity = averageCapacity,
                    NGODetails = ngoDetails
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating NGO report for {StartDate} to {EndDate}", startDate, endDate);
                return new NGOReport
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalNGOs = 0,
                    ActiveNGOs = 0,
                    TotalDonationsReceived = 0,
                    TotalRequestsMade = 0,
                    AverageCapacity = 0,
                    NGODetails = new List<NGODetail>()
                };
            }
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
