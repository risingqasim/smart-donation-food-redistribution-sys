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
    /// Controller for NGO operations
    /// Only accessible to logged-in NGOs
    /// </summary>
    [Authorize(Roles = "NGO")]
    public class NGOController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NGOService _ngoService;
        private readonly ILogger<NGOController> _logger;

        public NGOController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            NGOService ngoService,
            ILogger<NGOController> logger)
        {
            _context = context;
            _userManager = userManager;
            _ngoService = ngoService;
            _logger = logger;
        }

        // GET: NGO/Dashboard
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ngo = await _context.NGOs
                .Include(n => n.User)
                .FirstOrDefaultAsync(n => n.UserId == userId);

            if (ngo == null)
            {
                return NotFound("NGO profile not found.");
            }

            var dashboard = new NGODashboardViewModel
            {
                NGO = ngo,
                AvailableDonations = await _context.Donations
                    .Include(d => d.Donor)
                    .Where(d => d.Status == "Available")
                    .AsNoTracking()
                    .ToListAsync(),
                MyRequests = await _context.DonationRequests
                    .Include(dr => dr.Donation)
                    .Include(dr => dr.Donation.Donor)
                    .Where(dr => dr.NGOId == ngo.Id)
                    .AsNoTracking()
                    .ToListAsync(),
                ClaimedDonations = await _context.Donations
                    .Include(d => d.Donor)
                    .Where(d => d.NGOId == ngo.Id)
                    .AsNoTracking()
                    .ToListAsync()
            };

            return View(dashboard);
        }

        // GET: NGO/AvailableDonations
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> AvailableDonations()
        {
            var donations = await _context.Donations
                .Include(d => d.Donor)
                .Where(d => d.Status == "Available")
                .AsNoTracking()
                .ToListAsync();

            return View(donations);
        }

        /// <summary>
        /// GET: NGO/ViewNearbyDonations
        /// Displays donations near the NGO's location using latitude and longitude
        /// </summary>
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ViewNearbyDonations(double? radiusKm = 50)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                // Get NGO with location
                var ngo = await _ngoService.GetNGOByUserIdAsync(userId);
                if (ngo == null)
                {
                    TempData["Error"] = "NGO profile not found.";
                    return RedirectToAction(nameof(Dashboard));
                }

                // Check if NGO has location coordinates
                if (!ngo.Latitude.HasValue || !ngo.Longitude.HasValue)
                {
                    TempData["Error"] = "NGO location coordinates are not set. Please update your profile with location information.";
                    return RedirectToAction(nameof(Dashboard));
                }

                // Get nearby donations using service layer
                var nearbyDonations = await _ngoService.GetNearbyDonationsAsync(
                    ngo.Latitude.Value,
                    ngo.Longitude.Value,
                    radiusKm ?? 50);

                ViewBag.RadiusKm = radiusKm ?? 50;
                ViewBag.NGOLocation = new { Latitude = ngo.Latitude.Value, Longitude = ngo.Longitude.Value };

                return View(nearbyDonations);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred while loading nearby donations: {ex.Message}";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        // GET: NGO/MyRequests
        public async Task<IActionResult> MyRequests()
        {
            return await ViewRequests();
        }

        /// <summary>
        /// GET: NGO/ViewRequests
        /// Displays all donation requests made by the NGO
        /// </summary>
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ViewRequests()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                // Get NGO
                var ngo = await _ngoService.GetNGOByUserIdAsync(userId);
                if (ngo == null)
                {
                    TempData["Error"] = "NGO profile not found.";
                    return RedirectToAction(nameof(Dashboard));
                }

                // Get requests using service layer
                var requests = await _ngoService.GetNGORequestsAsync(ngo.Id);

                return View(requests);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred while loading requests: {ex.Message}";
                return View(new List<DonationRequest>());
            }
        }

        /// <summary>
        /// POST: NGO/RequestDonation
        /// Creates a donation request for a specific donation
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestDonation(int donationId, string message)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["Error"] = "User authentication failed. Please log in again.";
                    return RedirectToAction(nameof(Dashboard));
                }

                // Validate input
                if (donationId <= 0)
                {
                    TempData["Error"] = "Invalid donation ID.";
                    return RedirectToAction(nameof(ViewNearbyDonations));
                }

                if (string.IsNullOrWhiteSpace(message))
                {
                    TempData["Error"] = "Please provide a message for your request.";
                    return RedirectToAction(nameof(ViewNearbyDonations));
                }

                // Get NGO
                var ngo = await _ngoService.GetNGOByUserIdAsync(userId);
                if (ngo == null)
                {
                    TempData["Error"] = "NGO profile not found. Please complete your profile setup.";
                    return RedirectToAction(nameof(Dashboard));
                }

                // Create request using service layer (validation happens in service)
                var result = await _ngoService.CreateDonationRequestAsync(donationId, ngo.Id, message);

                if (!result.Success)
                {
                    TempData["Error"] = result.ErrorMessage ?? "Failed to create donation request.";
                    return RedirectToAction(nameof(ViewNearbyDonations));
                }

                TempData["Success"] = result.Message ?? "Donation request submitted successfully! The donor has been notified.";
                return RedirectToAction(nameof(ViewRequests));
            }
            catch (ArgumentException ex)
            {
                TempData["Error"] = ex.Message;
                _logger.LogWarning(ex, "Invalid input for donation request");
                return RedirectToAction(nameof(ViewNearbyDonations));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An unexpected error occurred while submitting the request. Please try again.";
                _logger.LogError(ex, "Unexpected error creating donation request");
                return RedirectToAction(nameof(ViewNearbyDonations));
            }
        }

        // GET: NGO/DonationDetails/5
        public async Task<IActionResult> DonationDetails(int id)
        {
            var donation = await _context.Donations
                .Include(d => d.Donor)
                .Include(d => d.NGO)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);

            if (donation == null)
            {
                return NotFound();
            }

            return View(donation);
        }
    }

    public class NGODashboardViewModel
    {
        public NGO NGO { get; set; } = new NGO();
        public List<Donation> AvailableDonations { get; set; } = new List<Donation>();
        public List<DonationRequest> MyRequests { get; set; } = new List<DonationRequest>();
        public List<Donation> ClaimedDonations { get; set; } = new List<Donation>();
    }
}
