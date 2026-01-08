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
    /// Controller for donation management
    /// Requires authentication - role restrictions on specific actions
    /// </summary>
    [Authorize]
    public class DonationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly NotificationService _notificationService;

        public DonationsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration, NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
            _notificationService = notificationService;
        }

        // GET: Donations
        /// <summary>
        /// View all donations - accessible to all authenticated users
        /// </summary>
        [Authorize(Roles = "Donor,NGO,Admin")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Index()
        {
            var donations = await _context.Donations
                .Include(d => d.Donor)
                .Include(d => d.NGO)
                .AsNoTracking()
                .ToListAsync();
            return View(donations);
        }

        // GET: Donations/Details/5
        /// <summary>
        /// View donation details - accessible to all authenticated users
        /// </summary>
        [Authorize(Roles = "Donor,NGO,Admin")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var donation = await _context.Donations
                .Include(d => d.Donor)
                .Include(d => d.NGO)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (donation == null)
            {
                return NotFound();
            }

            return View(donation);
        }

        // GET: Donations/Create
        [Authorize(Roles = "Donor,Admin")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult Create()
        {
            ViewBag.GoogleMapsApiKey = _configuration["GoogleMaps:ApiKey"];
            return View();
        }

        // POST: Donations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Donor,Admin")]
        public async Task<IActionResult> Create([Bind("Title,Description,FoodType,Quantity,Unit,ExpiryDate,PickupAddress,ImageUrl,Location")] Donation donation)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                donation.DonorId = userId!;
                donation.CreatedAt = DateTime.UtcNow;

                _context.Add(donation);
                await _context.SaveChangesAsync();

                // Send real-time notification to NGOs about new donation
                await _notificationService.NotifyNewDonationAsync(donation);

                return RedirectToAction(nameof(Index));
            }
            return View(donation);
        }

        // GET: Donations/Edit/5
        [Authorize(Roles = "Donor,Admin")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var donation = await _context.Donations.FindAsync(id);
            if (donation == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (donation.DonorId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            return View(donation);
        }

        // POST: Donations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Donor,Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,FoodType,Quantity,Unit,ExpiryDate,PickupAddress,ImageUrl,Location,DonorId")] Donation donation)
        {
            if (id != donation.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (donation.DonorId != userId && !User.IsInRole("Admin"))
                    {
                        return Forbid();
                    }

                    donation.UpdatedAt = DateTime.UtcNow;
                    _context.Update(donation);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DonationExists(donation.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(donation);
        }

        // GET: Donations/Delete/5
        [Authorize(Roles = "Donor,Admin")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var donation = await _context.Donations
                .Include(d => d.Donor)
                .Include(d => d.NGO)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (donation == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (donation.DonorId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            return View(donation);
        }

        // POST: Donations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Donor,Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var donation = await _context.Donations.FindAsync(id);
            if (donation != null)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (donation.DonorId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                _context.Donations.Remove(donation);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DonationExists(int id)
        {
            return _context.Donations.Any(e => e.Id == id);
        }
    }
}
