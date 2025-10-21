using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    public class DonationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DonationsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetDonations([FromQuery] string? category = null, [FromQuery] string? status = null)
        {
            var query = _context.Donations
                .Include(d => d.Donor)
                .Include(d => d.NGO)
                .AsNoTracking();

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(d => d.FoodType == category);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(d => d.Status == status);
            }

            var donations = await query.ToListAsync();
            return Ok(donations);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDonation(int id)
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

            return Ok(donation);
        }

        [HttpPost]
        [Authorize(Roles = "Donor,Admin")]
        public async Task<IActionResult> CreateDonation([FromBody] Donation donation)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            donation.DonorId = userId!;
            donation.CreatedAt = DateTime.UtcNow;

            _context.Donations.Add(donation);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDonation), new { id = donation.Id }, donation);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Donor,Admin")]
        public async Task<IActionResult> UpdateDonation(int id, [FromBody] Donation donation)
        {
            if (id != donation.Id)
            {
                return BadRequest();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existingDonation = await _context.Donations.FindAsync(id);

            if (existingDonation == null)
            {
                return NotFound();
            }

            // Check if user owns the donation or is admin
            if (existingDonation.DonorId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            existingDonation.Title = donation.Title;
            existingDonation.Description = donation.Description;
            existingDonation.FoodType = donation.FoodType;
            existingDonation.Quantity = donation.Quantity;
            existingDonation.Unit = donation.Unit;
            existingDonation.ExpiryDate = donation.ExpiryDate;
            existingDonation.PickupAddress = donation.PickupAddress;
            existingDonation.ImageUrl = donation.ImageUrl;
            existingDonation.Location = donation.Location;
            existingDonation.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DonationExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Donor,Admin")]
        public async Task<IActionResult> DeleteDonation(int id)
        {
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

            _context.Donations.Remove(donation);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool DonationExists(int id)
        {
            return _context.Donations.Any(e => e.Id == id);
        }
    }
}
