using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDonationSystem.Data;
using SmartDonationSystem.Models;
using System.Security.Claims;

namespace SmartDonationSystem.Controllers
{
    [Authorize(Roles = "NGO")]
    public class NGOController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NGOController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: NGO/Dashboard
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
        public async Task<IActionResult> AvailableDonations()
        {
            var donations = await _context.Donations
                .Include(d => d.Donor)
                .Where(d => d.Status == "Available")
                .AsNoTracking()
                .ToListAsync();

            return View(donations);
        }

        // GET: NGO/MyRequests
        public async Task<IActionResult> MyRequests()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ngo = await _context.NGOs.FirstOrDefaultAsync(n => n.UserId == userId);

            if (ngo == null)
            {
                return NotFound("NGO profile not found.");
            }

            var requests = await _context.DonationRequests
                .Include(dr => dr.Donation)
                .Include(dr => dr.Donation.Donor)
                .Where(dr => dr.NGOId == ngo.Id)
                .AsNoTracking()
                .ToListAsync();

            return View(requests);
        }

        // POST: NGO/RequestDonation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestDonation(int donationId, string message)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ngo = await _context.NGOs.FirstOrDefaultAsync(n => n.UserId == userId);

            if (ngo == null)
            {
                return NotFound("NGO profile not found.");
            }

            var donation = await _context.Donations.FindAsync(donationId);
            if (donation == null || donation.Status != "Available")
            {
                TempData["Error"] = "Donation not available for request.";
                return RedirectToAction(nameof(AvailableDonations));
            }

            // Check if NGO already has a pending request for this donation
            var existingRequest = await _context.DonationRequests
                .FirstOrDefaultAsync(dr => dr.DonationId == donationId && dr.NGOId == ngo.Id);

            if (existingRequest != null)
            {
                TempData["Error"] = "You have already requested this donation.";
                return RedirectToAction(nameof(AvailableDonations));
            }

            var request = new DonationRequest
            {
                DonationId = donationId,
                NGOId = ngo.Id,
                Message = message,
                Status = "Pending"
            };

            _context.DonationRequests.Add(request);
            await _context.SaveChangesAsync();

            // Create notification for donor
            var notification = new Notification
            {
                UserId = donation.DonorId,
                Title = "New Donation Request",
                Message = $"Your donation '{donation.Title}' has been requested by {ngo.Name}.",
                Type = "Info",
                RelatedEntityId = donationId,
                RelatedEntityType = "Donation"
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Donation request submitted successfully.";
            return RedirectToAction(nameof(MyRequests));
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
