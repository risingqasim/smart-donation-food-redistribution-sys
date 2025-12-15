using Microsoft.EntityFrameworkCore;
using SmartDonationSystem.Data;
using SmartDonationSystem.Exceptions;
using SmartDonationSystem.Models;

namespace SmartDonationSystem.Services
{
    /// <summary>
    /// Service layer for NGO operations
    /// </summary>
    public class NGOService
    {
        private readonly ApplicationDbContext _context;
        private readonly GoogleMapsService _googleMapsService;
        private readonly NotificationService _notificationService;
        private readonly ValidationService _validationService;
        private readonly ILogger<NGOService> _logger;

        public NGOService(
            ApplicationDbContext context,
            GoogleMapsService googleMapsService,
            NotificationService notificationService,
            ValidationService validationService,
            ILogger<NGOService> logger)
        {
            _context = context;
            _googleMapsService = googleMapsService;
            _notificationService = notificationService;
            _validationService = validationService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the NGO associated with a user ID
        /// </summary>
        public async Task<NGO?> GetNGOByUserIdAsync(string userId)
        {
            return await _context.NGOs
                .Include(n => n.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.UserId == userId);
        }

        /// <summary>
        /// Finds nearby donations based on NGO's latitude and longitude
        /// </summary>
        public async Task<List<NearbyDonationViewModel>> GetNearbyDonationsAsync(
            double latitude, 
            double longitude, 
            double radiusKm = 50)
        {
            var ngoLocation = new Location 
            { 
                Latitude = latitude, 
                Longitude = longitude 
            };

            // Get all available donations with donor information
            var donations = await _context.Donations
                .Include(d => d.Donor)
                .Where(d => d.Status == "Available")
                .AsNoTracking()
                .ToListAsync();

            var nearbyDonations = new List<NearbyDonationViewModel>();

            foreach (var donation in donations)
            {
                // Check if donor has location coordinates
                if (donation.Donor?.Latitude == null || donation.Donor.Longitude == null)
                {
                    continue; // Skip donations without location data
                }

                var donorLocation = new Location
                {
                    Latitude = donation.Donor.Latitude.Value,
                    Longitude = donation.Donor.Longitude.Value
                };

                // Calculate distance using Haversine formula
                var distance = _googleMapsService.CalculateHaversineDistance(ngoLocation, donorLocation);

                if (distance <= radiusKm)
                {
                    nearbyDonations.Add(new NearbyDonationViewModel
                    {
                        Donation = donation,
                        DistanceKm = Math.Round(distance, 2),
                        EstimatedDurationMinutes = (int)(distance * 1.5) // Approximate driving time
                    });
                }
            }

            // Sort by distance (closest first)
            return nearbyDonations.OrderBy(d => d.DistanceKm).ToList();
        }

        /// <summary>
        /// Creates a donation request with validation
        /// </summary>
        public async Task<DonationRequestResult> CreateDonationRequestAsync(
            int donationId, 
            int ngoId, 
            string message)
        {
            try
            {
                // Validate input parameters
                if (donationId <= 0)
                {
                    throw new ArgumentException("Donation ID must be greater than 0.", nameof(donationId));
                }

                if (ngoId <= 0)
                {
                    throw new ArgumentException("NGO ID must be greater than 0.", nameof(ngoId));
                }

                if (string.IsNullOrWhiteSpace(message))
                {
                    throw new ArgumentException("Message cannot be null or empty.", nameof(message));
                }

                // Check if donation exists
                var donation = await _context.Donations
                    .Include(d => d.Donor)
                    .FirstOrDefaultAsync(d => d.Id == donationId);

                if (donation == null)
                {
                    throw new DonationNotFoundException(donationId);
                }

                // Check if NGO exists
                var ngo = await _context.NGOs.FirstOrDefaultAsync(n => n.Id == ngoId);
                if (ngo == null)
                {
                    throw new ArgumentException($"NGO with ID {ngoId} not found.", nameof(ngoId));
                }

                // Validate donation requestability
                var requestabilityValidation = _validationService.ValidateDonationRequestability(donation, ngoId);
                if (!requestabilityValidation.IsValid)
                {
                    throw new DonationRequestValidationException("Donation cannot be requested.", requestabilityValidation.Errors);
                }

                // Check if NGO already has a pending request for this donation
                var existingRequest = await _context.DonationRequests
                    .FirstOrDefaultAsync(dr => dr.DonationId == donationId && dr.NGOId == ngoId && dr.Status == "Pending");

                if (existingRequest != null)
                {
                    throw new DuplicateDonationRequestException(donationId, ngoId);
                }

                // Create the request
                var request = new DonationRequest
                {
                    DonationId = donationId,
                    NGOId = ngoId,
                    Message = message.Trim(),
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Validate request
                var validationResult = _validationService.ValidateDonationRequest(request);
                if (!validationResult.IsValid)
                {
                    throw new DonationRequestValidationException("Donation request validation failed.", validationResult.Errors);
                }

                _context.DonationRequests.Add(request);
                await _context.SaveChangesAsync();

                // Create notification for donor
                try
                {
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
                }
                catch (Exception ex)
                {
                    // Log notification error but don't fail the request creation
                    _logger.LogWarning(ex, "Failed to create notification for donation request {RequestId}", request.Id);
                }

                _logger.LogInformation("Donation request {RequestId} created successfully for donation {DonationId} by NGO {NGOId}", 
                    request.Id, donationId, ngoId);

                return new DonationRequestResult
                {
                    Success = true,
                    Request = request,
                    Message = "Donation request created successfully."
                };
            }
            catch (DonationRequestException ex)
            {
                _logger.LogWarning(ex, "Donation request creation failed for donation {DonationId} by NGO {NGOId}", donationId, ngoId);
                return new DonationRequestResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
            catch (DonationException ex)
            {
                _logger.LogWarning(ex, "Donation request creation failed for donation {DonationId} by NGO {NGOId}", donationId, ngoId);
                return new DonationRequestResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating donation request for donation {DonationId} by NGO {NGOId}", donationId, ngoId);
                return new DonationRequestResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while creating the donation request. Please try again."
                };
            }
        }

        /// <summary>
        /// Gets all donation requests for an NGO
        /// </summary>
        public async Task<List<DonationRequest>> GetNGORequestsAsync(int ngoId)
        {
            return await _context.DonationRequests
                .Include(dr => dr.Donation)
                    .ThenInclude(d => d!.Donor)
                .Where(dr => dr.NGOId == ngoId)
                .OrderByDescending(dr => dr.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Approves a donation request with validation
        /// </summary>
        public async Task<DonationRequestResult> ApproveDonationRequestAsync(
            int requestId, 
            string userId, 
            string? responseMessage = null,
            bool isAdmin = false)
        {
            try
            {
                var request = await _context.DonationRequests
                    .Include(dr => dr.Donation)
                    .Include(dr => dr.NGO)
                    .FirstOrDefaultAsync(dr => dr.Id == requestId);

                if (request == null)
                {
                    throw new DonationRequestNotFoundException(requestId);
                }

                // Validate request response
                var validationResult = _validationService.ValidateRequestResponse(request, userId, isAdmin);
                if (!validationResult.IsValid)
                {
                    throw new DonationRequestValidationException("Cannot approve request.", validationResult.Errors);
                }

                // Approve the request
                request.Status = "Approved";
                request.ResponseMessage = responseMessage?.Trim();
                request.RespondedAt = DateTime.UtcNow;
                request.UpdatedAt = DateTime.UtcNow;

                // Update donation status
                request.Donation!.Status = "Reserved";
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
                try
                {
                    var notification = new Notification
                    {
                        UserId = request.NGO!.UserId!,
                        Title = "Donation Request Approved",
                        Message = $"Your request for '{request.Donation.Title}' has been approved.",
                        Type = "Success",
                        RelatedEntityId = request.DonationId,
                        RelatedEntityType = "Donation"
                    };

                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create notification for approved request {RequestId}", requestId);
                }

                _logger.LogInformation("Donation request {RequestId} approved by user {UserId}", requestId, userId);

                return new DonationRequestResult
                {
                    Success = true,
                    Request = request,
                    Message = "Donation request approved successfully."
                };
            }
            catch (DonationRequestException ex)
            {
                _logger.LogWarning(ex, "Failed to approve donation request {RequestId}", requestId);
                return new DonationRequestResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving donation request {RequestId}", requestId);
                return new DonationRequestResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while approving the request. Please try again."
                };
            }
        }

        /// <summary>
        /// Rejects a donation request with validation
        /// </summary>
        public async Task<DonationRequestResult> RejectDonationRequestAsync(
            int requestId, 
            string userId, 
            string? responseMessage = null,
            bool isAdmin = false)
        {
            try
            {
                var request = await _context.DonationRequests
                    .Include(dr => dr.Donation)
                    .Include(dr => dr.NGO)
                    .FirstOrDefaultAsync(dr => dr.Id == requestId);

                if (request == null)
                {
                    throw new DonationRequestNotFoundException(requestId);
                }

                // Validate request response
                var validationResult = _validationService.ValidateRequestResponse(request, userId, isAdmin);
                if (!validationResult.IsValid)
                {
                    throw new DonationRequestValidationException("Cannot reject request.", validationResult.Errors);
                }

                // Reject the request
                request.Status = "Rejected";
                request.ResponseMessage = responseMessage?.Trim();
                request.RespondedAt = DateTime.UtcNow;
                request.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Create notification for NGO
                try
                {
                    var notification = new Notification
                    {
                        UserId = request.NGO!.UserId!,
                        Title = "Donation Request Rejected",
                        Message = $"Your request for '{request.Donation!.Title}' has been rejected.",
                        Type = "Warning",
                        RelatedEntityId = request.DonationId,
                        RelatedEntityType = "Donation"
                    };

                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create notification for rejected request {RequestId}", requestId);
                }

                _logger.LogInformation("Donation request {RequestId} rejected by user {UserId}", requestId, userId);

                return new DonationRequestResult
                {
                    Success = true,
                    Request = request,
                    Message = "Donation request rejected."
                };
            }
            catch (DonationRequestException ex)
            {
                _logger.LogWarning(ex, "Failed to reject donation request {RequestId}", requestId);
                return new DonationRequestResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting donation request {RequestId}", requestId);
                return new DonationRequestResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while rejecting the request. Please try again."
                };
            }
        }
    }

    /// <summary>
    /// View model for nearby donations with distance information
    /// </summary>
    public class NearbyDonationViewModel
    {
        public Donation Donation { get; set; } = null!;
        public double DistanceKm { get; set; }
        public int EstimatedDurationMinutes { get; set; }
    }

    /// <summary>
    /// Result of donation request creation
    /// </summary>
    public class DonationRequestResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Message { get; set; }
        public DonationRequest? Request { get; set; }
    }
}

