using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDonationSystem.Data;
using SmartDonationSystem.Models;
using System.Security.Claims;

namespace SmartDonationSystem.Controllers
{
    [Authorize(Roles = "Donor")]
    public class DonorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DonorController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Donor/Dashboard
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
                    .Where(dr => dr.Donation.DonorId == userId && dr.Status == "Pending")
                    .AsNoTracking()
                    .ToListAsync()
            };

            return View(dashboard);
        }

        // GET: Donor/MyDonations
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
        public async Task<IActionResult> DonationRequests()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var requests = await _context.DonationRequests
                .Include(dr => dr.NGO)
                .Include(dr => dr.Donation)
                .Where(dr => dr.Donation.DonorId == userId)
                .AsNoTracking()
                .ToListAsync();

            return View(requests);
        }

        // POST: Donor/ApproveRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRequest(int requestId, string responseMessage = "")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var request = await _context.DonationRequests
                .Include(dr => dr.Donation)
                .Include(dr => dr.NGO)
                .FirstOrDefaultAsync(dr => dr.Id == requestId && dr.Donation.DonorId == userId);

            if (request == null)
            {
                return NotFound();
            }

            if (request.Donation.Status != "Available")
            {
                TempData["Error"] = "This donation is no longer available.";
                return RedirectToAction(nameof(DonationRequests));
            }

            // Approve the request
            request.Status = "Approved";
            request.ResponseMessage = responseMessage;
            request.RespondedAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;

            // Update donation status
            request.Donation.Status = "Reserved";
            request.Donation.NGOId = request.NGOId;
            request.Donation.UpdatedAt = DateTime.UtcNow;

            // Reject other pending requests for the same donation
            var otherRequests = await _context.DonationRequests
                .Where(dr => dr.DonationId == request.DonationId && dr.Id != requestId && dr.Status == "Pending")
                .ToListAsync();

            foreach (var otherRequest in otherRequests)
            {
                otherRequest.Status = "Rejected";
                otherRequest.ResponseMessage = "Donation has been approved for another organization.";
                otherRequest.RespondedAt = DateTime.UtcNow;
                otherRequest.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Create notification for NGO
            var notification = new Notification
            {
                UserId = request.NGO.UserId!,
                Title = "Donation Request Approved",
                Message = $"Your request for '{request.Donation.Title}' has been approved by the donor.",
                Type = "Success",
                RelatedEntityId = request.DonationId,
                RelatedEntityType = "Donation"
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Donation request approved successfully.";
            return RedirectToAction(nameof(DonationRequests));
        }

        // POST: Donor/RejectRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRequest(int requestId, string responseMessage = "")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var request = await _context.DonationRequests
                .Include(dr => dr.Donation)
                .Include(dr => dr.NGO)
                .FirstOrDefaultAsync(dr => dr.Id == requestId && dr.Donation.DonorId == userId);

            if (request == null)
            {
                return NotFound();
            }

            request.Status = "Rejected";
            request.ResponseMessage = responseMessage;
            request.RespondedAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Create notification for NGO
            var notification = new Notification
            {
                UserId = request.NGO.UserId!,
                Title = "Donation Request Rejected",
                Message = $"Your request for '{request.Donation.Title}' has been rejected by the donor.",
                Type = "Warning",
                RelatedEntityId = request.DonationId,
                RelatedEntityType = "Donation"
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Donation request rejected.";
            return RedirectToAction(nameof(DonationRequests));
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
