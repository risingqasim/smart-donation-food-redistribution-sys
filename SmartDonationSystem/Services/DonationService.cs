using Microsoft.EntityFrameworkCore;
using SmartDonationSystem.Data;
using SmartDonationSystem.Exceptions;
using SmartDonationSystem.Models;

namespace SmartDonationSystem.Services
{
    /// <summary>
    /// Service layer for donation operations
    /// </summary>
    public class DonationService
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;
        private readonly ValidationService _validationService;
        private readonly ILogger<DonationService> _logger;

        public DonationService(
            ApplicationDbContext context, 
            NotificationService notificationService,
            ValidationService validationService,
            ILogger<DonationService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _validationService = validationService;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new donation with validation
        /// </summary>
        public async Task<Donation> CreateDonationAsync(Donation donation, string donorId)
        {
            try
            {
                // Validate donation
                var validationResult = _validationService.ValidateDonation(donation);
                if (!validationResult.IsValid)
                {
                    throw new DonationValidationException("Donation validation failed.", validationResult.Errors);
                }

                // Validate donor exists
                var donorExists = await _context.Users.AnyAsync(u => u.Id == donorId);
                if (!donorExists)
                {
                    throw new DonationPermissionException($"Donor with ID {donorId} not found.");
                }

                donation.DonorId = donorId;
                donation.Status = "Available";
                donation.CreatedAt = DateTime.UtcNow;
                donation.UpdatedAt = DateTime.UtcNow;

                _context.Donations.Add(donation);
                await _context.SaveChangesAsync();

                // Reload donation with related entities for notification
                var createdDonation = await _context.Donations
                    .Include(d => d.Donor)
                    .FirstOrDefaultAsync(d => d.Id == donation.Id);

                if (createdDonation != null)
                {
                    try
                    {
                        // Send real-time notification to NGOs about new donation
                        await _notificationService.NotifyNewDonationAsync(createdDonation);
                    }
                    catch (Exception ex)
                    {
                        // Log notification error but don't fail the donation creation
                        _logger.LogWarning(ex, "Failed to send notification for donation {DonationId}", donation.Id);
                    }
                }

                _logger.LogInformation("Donation {DonationId} created successfully by donor {DonorId}", donation.Id, donorId);
                return donation;
            }
            catch (DonationException)
            {
                throw; // Re-throw donation-specific exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating donation for donor {DonorId}", donorId);
                throw new DonationException("An error occurred while creating the donation.", ex);
            }
        }

        /// <summary>
        /// Gets all donations for a specific donor
        /// </summary>
        public async Task<List<Donation>> GetDonorDonationsAsync(string donorId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(donorId))
                {
                    throw new ArgumentException("Donor ID cannot be null or empty.", nameof(donorId));
                }

                return await _context.Donations
                    .Include(d => d.NGO)
                    .Include(d => d.DonationRequests)
                        .ThenInclude(dr => dr.NGO)
                    .Where(d => d.DonorId == donorId)
                    .OrderByDescending(d => d.CreatedAt)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving donations for donor {DonorId}", donorId);
                throw new DonationException("An error occurred while retrieving donations.", ex);
            }
        }

        /// <summary>
        /// Gets a donation by ID if it belongs to the donor
        /// </summary>
        public async Task<Donation> GetDonorDonationByIdAsync(int donationId, string donorId)
        {
            try
            {
                if (donationId <= 0)
                {
                    throw new ArgumentException("Donation ID must be greater than 0.", nameof(donationId));
                }

                if (string.IsNullOrWhiteSpace(donorId))
                {
                    throw new ArgumentException("Donor ID cannot be null or empty.", nameof(donorId));
                }

                var donation = await _context.Donations
                    .Include(d => d.NGO)
                    .Include(d => d.DonationRequests)
                        .ThenInclude(dr => dr.NGO)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == donationId && d.DonorId == donorId);

                if (donation == null)
                {
                    throw new DonationNotFoundException(donationId);
                }

                return donation;
            }
            catch (DonationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving donation {DonationId} for donor {DonorId}", donationId, donorId);
                throw new DonationException("An error occurred while retrieving the donation.", ex);
            }
        }

        /// <summary>
        /// Updates a donation with validation
        /// </summary>
        public async Task<Donation> UpdateDonationAsync(int donationId, Donation updatedDonation, string userId, bool isAdmin = false)
        {
            try
            {
                var existingDonation = await _context.Donations
                    .FirstOrDefaultAsync(d => d.Id == donationId);

                if (existingDonation == null)
                {
                    throw new DonationNotFoundException(donationId);
                }

                // Validate update
                var validationResult = _validationService.ValidateDonationUpdate(existingDonation, updatedDonation, userId, isAdmin);
                if (!validationResult.IsValid)
                {
                    throw new DonationValidationException("Donation update validation failed.", validationResult.Errors);
                }

                // Update properties
                existingDonation.Title = updatedDonation.Title;
                existingDonation.Description = updatedDonation.Description;
                existingDonation.FoodType = updatedDonation.FoodType;
                existingDonation.Quantity = updatedDonation.Quantity;
                existingDonation.Unit = updatedDonation.Unit;
                existingDonation.ExpiryDate = updatedDonation.ExpiryDate;
                existingDonation.PickupAddress = updatedDonation.PickupAddress;
                existingDonation.ImageUrl = updatedDonation.ImageUrl;
                existingDonation.Location = updatedDonation.Location;
                existingDonation.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Donation {DonationId} updated successfully by user {UserId}", donationId, userId);
                return existingDonation;
            }
            catch (DonationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating donation {DonationId}", donationId);
                throw new DonationException("An error occurred while updating the donation.", ex);
            }
        }

        /// <summary>
        /// Deletes a donation with validation
        /// </summary>
        public async Task DeleteDonationAsync(int donationId, string userId, bool isAdmin = false)
        {
            try
            {
                var donation = await _context.Donations
                    .Include(d => d.DonationRequests)
                    .FirstOrDefaultAsync(d => d.Id == donationId);

                if (donation == null)
                {
                    throw new DonationNotFoundException(donationId);
                }

                // Check ownership (unless admin)
                if (!isAdmin && donation.DonorId != userId)
                {
                    throw new DonationPermissionException("You do not have permission to delete this donation.");
                }

                // Check if donation can be deleted based on status
                if (donation.Status == "Collected")
                {
                    throw new DonationOperationNotAllowedException("Delete", donation.Status);
                }

                // Check if there are approved requests
                if (donation.DonationRequests.Any(dr => dr.Status == "Approved"))
                {
                    throw new DonationOperationNotAllowedException("Delete", "Cannot delete donation with approved requests.");
                }

                _context.Donations.Remove(donation);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Donation {DonationId} deleted successfully by user {UserId}", donationId, userId);
            }
            catch (DonationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting donation {DonationId}", donationId);
                throw new DonationException("An error occurred while deleting the donation.", ex);
            }
        }

        /// <summary>
        /// Checks if a donation belongs to a specific donor
        /// </summary>
        public async Task<bool> IsDonationOwnerAsync(int donationId, string donorId)
        {
            try
            {
                return await _context.Donations
                    .AnyAsync(d => d.Id == donationId && d.DonorId == donorId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking donation ownership for donation {DonationId}", donationId);
                return false;
            }
        }
    }
}

