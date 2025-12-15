using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDonationSystem.Data;
using SmartDonationSystem.Models;
using SmartDonationSystem.Services;
using System.Security.Claims;

namespace SmartDonationSystem.Controllers.Api
{
    /// <summary>
    /// API Controller for donation request operations
    /// Requires authentication - role-based filtering applied
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Donor,NGO,Admin")]
    public class DonationRequestsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;

        public DonationRequestsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // GET: api/donationrequests
        [HttpGet]
        public async Task<IActionResult> GetDonationRequests()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);
            var roles = await _userManager.GetRolesAsync(user!);

            IQueryable<DonationRequest> query = _context.DonationRequests
                .Include(dr => dr.Donation)
                .Include(dr => dr.NGO)
                .Include(dr => dr.NGO!.User)
                .AsNoTracking();

            // Filter based on role
            if (roles.Contains("Donor"))
            {
                query = query.Where(dr => dr.Donation!.DonorId == userId);
            }
            else if (roles.Contains("NGO"))
            {
                var ngo = await _context.NGOs.FirstOrDefaultAsync(n => n.UserId == userId);
                if (ngo != null)
                {
                    query = query.Where(dr => dr.NGOId == ngo.Id);
                }
                else
                {
                    return NotFound("NGO profile not found.");
                }
            }
            // Admin can see all requests

            var requests = await query.ToListAsync();
            return Ok(requests);
        }

        // GET: api/donationrequests/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDonationRequest(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);
            var roles = await _userManager.GetRolesAsync(user!);

            var request = await _context.DonationRequests
                .Include(dr => dr.Donation)
                .Include(dr => dr.NGO)
                .Include(dr => dr.NGO!.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(dr => dr.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            // Check access permissions
            if (roles.Contains("Donor") && request.Donation!.DonorId != userId)
            {
                return Forbid();
            }
            else if (roles.Contains("NGO"))
            {
                var ngo = await _context.NGOs.FirstOrDefaultAsync(n => n.UserId == userId);
                if (ngo == null || request.NGOId != ngo.Id)
                {
                    return Forbid();
                }
            }

            return Ok(request);
        }

        // POST: api/donationrequests
        [HttpPost]
        [Authorize(Roles = "NGO")]
        public async Task<IActionResult> CreateDonationRequest([FromBody] CreateDonationRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ngo = await _context.NGOs.FirstOrDefaultAsync(n => n.UserId == userId);

            if (ngo == null)
            {
                return NotFound("NGO profile not found.");
            }

            var donation = await _context.Donations.FindAsync(dto.DonationId);
            if (donation == null || donation.Status != "Available")
            {
                return BadRequest("Donation not available for request.");
            }

            // Check if NGO already has a pending request for this donation
            var existingRequest = await _context.DonationRequests
                .FirstOrDefaultAsync(dr => dr.DonationId == dto.DonationId && dr.NGOId == ngo.Id);

            if (existingRequest != null)
            {
                return BadRequest("You have already requested this donation.");
            }

            var request = new DonationRequest
            {
                DonationId = dto.DonationId,
                NGOId = ngo.Id,
                Message = dto.Message,
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
                RelatedEntityId = donation.Id,
                RelatedEntityType = "Donation"
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDonationRequest), new { id = request.Id }, request);
        }

        // PUT: api/donationrequests/{id}/approve
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Donor,Admin")]
        public async Task<IActionResult> ApproveRequest(int id, [FromBody] RespondToRequestDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);
            var roles = await _userManager.GetRolesAsync(user!);

            var request = await _context.DonationRequests
                .Include(dr => dr.Donation)
                .Include(dr => dr.NGO)
                .FirstOrDefaultAsync(dr => dr.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            // Check permissions
            if (roles.Contains("Donor") && request.Donation!.DonorId != userId)
            {
                return Forbid();
            }

            if (request.Donation!.Status != "Available")
            {
                return BadRequest("This donation is no longer available.");
            }

            // Approve the request
            request.Status = "Approved";
            request.ResponseMessage = dto.ResponseMessage;
            request.RespondedAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;

            // Update donation status
            request.Donation.Status = "Reserved";
            request.Donation.NGOId = request.NGOId;
            request.Donation.UpdatedAt = DateTime.UtcNow;

            // Reject other pending requests for the same donation
            var otherRequests = await _context.DonationRequests
                .Where(dr => dr.DonationId == request.DonationId && dr.Id != id && dr.Status == "Pending")
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

            return Ok(request);
        }

        // PUT: api/donationrequests/{id}/reject
        [HttpPut("{id}/reject")]
        [Authorize(Roles = "Donor,Admin")]
        public async Task<IActionResult> RejectRequest(int id, [FromBody] RespondToRequestDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);
            var roles = await _userManager.GetRolesAsync(user!);

            var request = await _context.DonationRequests
                .Include(dr => dr.Donation)
                .Include(dr => dr.NGO)
                .FirstOrDefaultAsync(dr => dr.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            // Check permissions
            if (roles.Contains("Donor") && request.Donation!.DonorId != userId)
            {
                return Forbid();
            }

            request.Status = "Rejected";
            request.ResponseMessage = dto.ResponseMessage;
            request.RespondedAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Create notification for NGO
            var notification = new Notification
            {
                UserId = request.NGO.UserId!,
                Title = "Donation Request Rejected",
                Message = $"Your request for '{request.Donation!.Title}' has been rejected by the donor.",
                Type = "Warning",
                RelatedEntityId = request.DonationId,
                RelatedEntityType = "Donation"
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return Ok(request);
        }

        // DELETE: api/donationrequests/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "NGO,Admin")]
        public async Task<IActionResult> DeleteDonationRequest(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);
            var roles = await _userManager.GetRolesAsync(user!);

            var request = await _context.DonationRequests
                .Include(dr => dr.Donation)
                .FirstOrDefaultAsync(dr => dr.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            // Check permissions
            if (roles.Contains("NGO"))
            {
                var ngo = await _context.NGOs.FirstOrDefaultAsync(n => n.UserId == userId);
                if (ngo == null || request.NGOId != ngo.Id)
                {
                    return Forbid();
                }
            }

            if (request.Status == "Approved")
            {
                return BadRequest("Cannot delete approved requests.");
            }

            _context.DonationRequests.Remove(request);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    public class CreateDonationRequestDto
    {
        public int DonationId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class RespondToRequestDto
    {
        public string ResponseMessage { get; set; } = string.Empty;
    }
}
