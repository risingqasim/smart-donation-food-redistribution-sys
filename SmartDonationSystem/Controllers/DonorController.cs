using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDonationSystem.Data;
using SmartDonationSystem.Exceptions;
using SmartDonationSystem.Models;
using SmartDonationSystem.Services;
using System.Security.Claims;

namespace SmartDonationSystem.Controllers
{
    [Authorize(Roles = "Donor")]
    public class DonorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NGOService _ngoService;
        private readonly ILogger<DonorController> _logger;

        public DonorController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            NGOService ngoService,
            ILogger<DonorController> logger)
        {
            _context = context;
            _userManager = userManager;
            _ngoService = ngoService;
            _logger = logger;
        }

        // GET: Donor/Dashboard
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var dashboard = new DonorDashboardViewModel
            {
                TotalDonations = await _context.Donations.CountAsync(d => d.DonorId == userId),
                AvailableDonations = await _context.Donations.CountAsync(d => d.DonorId == userId && d.Status == "Available"),
                ReservedDonations = await _context.Donations.CountAsync(d => d.DonorId == userId && d.Status == "Reserved"),
                CollectedDonations = await _context.Donations.CountAsync(d => d.DonorId == userId && d.Status == "Collected"),
                RecentDonations = await _context.Donations
                    .Include(d => d.NGO)
                    .Where(d => d.DonorId == userId)
                    .OrderByDescending(d => d.CreatedAt)
                    .Take(5)
                    .AsNoTracking()
                    .ToListAsync(),
                PendingRequests = await _context.DonationRequests
                    .Include(dr => dr.NGO)
                    .Include(dr => dr.Donation)
                    .Where(dr => dr.Donation != null && dr.Donation.DonorId == userId && dr.Status == "Pending")
                    .AsNoTracking()
                    .ToListAsync()
            };

            return View(dashboard);
        }

        // GET: Donor/MyDonations
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> MyDonations()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var donations = await _context.Donations
                .Include(d => d.NGO)
                .Include(d => d.DonationRequests)
                .Where(d => d.DonorId == userId)
                .AsNoTracking()
                .ToListAsync();

            return View(donations);
        }

        // GET: Donor/DonationRequests
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> DonationRequests()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var requests = await _context.DonationRequests
                .Include(dr => dr.NGO)
                .Include(dr => dr.Donation)
                .Where(dr => dr.Donation != null && dr.Donation.DonorId == userId)
                .AsNoTracking()
                .ToListAsync();

            return View(requests);
        }

        // POST: Donor/ApproveRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRequest(int requestId, string responseMessage = "")
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["Error"] = "User authentication failed. Please log in again.";
                    return RedirectToAction(nameof(DonationRequests));
                }

                if (requestId <= 0)
                {
                    TempData["Error"] = "Invalid request ID.";
                    return RedirectToAction(nameof(DonationRequests));
                }

                // Approve request using service layer (validation happens in service)
                var result = await _ngoService.ApproveDonationRequestAsync(requestId, userId, responseMessage, isAdmin: false);

                if (!result.Success)
                {
                    TempData["Error"] = result.ErrorMessage ?? "Failed to approve donation request.";
                    return RedirectToAction(nameof(DonationRequests));
                }

                TempData["Success"] = result.Message ?? "Donation request approved successfully.";
                return RedirectToAction(nameof(DonationRequests));
            }
            catch (DonationRequestNotFoundException ex)
            {
                TempData["Error"] = ex.Message;
                _logger.LogWarning(ex, "Donation request {RequestId} not found", requestId);
                return RedirectToAction(nameof(DonationRequests));
            }
            catch (DonationRequestValidationException ex)
            {
                TempData["Error"] = ex.Message;
                _logger.LogWarning(ex, "Validation failed for approving request {RequestId}", requestId);
                return RedirectToAction(nameof(DonationRequests));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An unexpected error occurred while approving the request. Please try again.";
                _logger.LogError(ex, "Unexpected error approving request {RequestId}", requestId);
                return RedirectToAction(nameof(DonationRequests));
            }
        }

        // POST: Donor/RejectRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRequest(int requestId, string responseMessage = "")
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["Error"] = "User authentication failed. Please log in again.";
                    return RedirectToAction(nameof(DonationRequests));
                }

                if (requestId <= 0)
                {
                    TempData["Error"] = "Invalid request ID.";
                    return RedirectToAction(nameof(DonationRequests));
                }

                // Reject request using service layer (validation happens in service)
                var result = await _ngoService.RejectDonationRequestAsync(requestId, userId, responseMessage, isAdmin: false);

                if (!result.Success)
                {
                    TempData["Error"] = result.ErrorMessage ?? "Failed to reject donation request.";
                    return RedirectToAction(nameof(DonationRequests));
                }

                TempData["Success"] = result.Message ?? "Donation request rejected.";
                return RedirectToAction(nameof(DonationRequests));
            }
            catch (DonationRequestNotFoundException ex)
            {
                TempData["Error"] = ex.Message;
                _logger.LogWarning(ex, "Donation request {RequestId} not found", requestId);
                return RedirectToAction(nameof(DonationRequests));
            }
            catch (DonationRequestValidationException ex)
            {
                TempData["Error"] = ex.Message;
                _logger.LogWarning(ex, "Validation failed for rejecting request {RequestId}", requestId);
                return RedirectToAction(nameof(DonationRequests));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An unexpected error occurred while rejecting the request. Please try again.";
                _logger.LogError(ex, "Unexpected error rejecting request {RequestId}", requestId);
                return RedirectToAction(nameof(DonationRequests));
            }
        }

        // POST: Donor/MarkAsCollected
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsCollected(int donationId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var donation = await _context.Donations
                .Include(d => d.NGO)
                .FirstOrDefaultAsync(d => d.Id == donationId && d.DonorId == userId);

            if (donation == null)
            {
                return NotFound();
            }

            if (donation.Status != "Reserved")
            {
                TempData["Error"] = "Only reserved donations can be marked as collected.";
                return RedirectToAction(nameof(MyDonations));
            }

            donation.Status = "Collected";
            donation.CollectedAt = DateTime.UtcNow;
            donation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Create notification for NGO
            if (donation.NGO != null)
            {
                var notification = new Notification
                {
                    UserId = donation.NGO.UserId!,
                    Title = "Donation Collected",
                    Message = $"The donation '{donation.Title}' has been marked as collected by the donor.",
                    Type = "Info",
                    RelatedEntityId = donationId,
                    RelatedEntityType = "Donation"
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Donation marked as collected successfully.";
            return RedirectToAction(nameof(MyDonations));
        }
    }

    public class DonorDashboardViewModel
    {
        public int TotalDonations { get; set; }
        public int AvailableDonations { get; set; }
        public int ReservedDonations { get; set; }
        public int CollectedDonations { get; set; }
        public List<Donation> RecentDonations { get; set; } = new List<Donation>();
        public List<DonationRequest> PendingRequests { get; set; } = new List<DonationRequest>();
    }
}
