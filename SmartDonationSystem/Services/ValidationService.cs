using SmartDonationSystem.Models;
using System.Text.RegularExpressions;

namespace SmartDonationSystem.Services
{
    /// <summary>
    /// Service for validating donation and request operations
    /// </summary>
    public class ValidationService
    {
        private readonly ILogger<ValidationService> _logger;

        public ValidationService(ILogger<ValidationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Validates a donation before creation or update
        /// </summary>
        public ValidationResult ValidateDonation(Donation donation, bool isUpdate = false)
        {
            var errors = new List<string>();

            // Title validation
            if (string.IsNullOrWhiteSpace(donation.Title))
            {
                errors.Add("Title is required.");
            }
            else if (donation.Title.Length > 200)
            {
                errors.Add("Title cannot exceed 200 characters.");
            }
            else if (donation.Title.Length < 3)
            {
                errors.Add("Title must be at least 3 characters long.");
            }

            // Description validation
            if (string.IsNullOrWhiteSpace(donation.Description))
            {
                errors.Add("Description is required.");
            }
            else if (donation.Description.Length > 1000)
            {
                errors.Add("Description cannot exceed 1000 characters.");
            }
            else if (donation.Description.Length < 10)
            {
                errors.Add("Description must be at least 10 characters long.");
            }

            // FoodType validation
            var validFoodTypes = new[] { "Vegetables", "Fruits", "Grains", "Dairy", "Meat", "Bakery", "Beverages", "Other" };
            if (string.IsNullOrWhiteSpace(donation.FoodType))
            {
                errors.Add("Food type is required.");
            }
            else if (!validFoodTypes.Contains(donation.FoodType))
            {
                errors.Add($"Food type must be one of: {string.Join(", ", validFoodTypes)}");
            }

            // Quantity validation
            if (donation.Quantity <= 0)
            {
                errors.Add("Quantity must be greater than 0.");
            }
            else if (donation.Quantity > 100000)
            {
                errors.Add("Quantity cannot exceed 100,000.");
            }

            // Unit validation
            if (!string.IsNullOrWhiteSpace(donation.Unit))
            {
                if (donation.Unit.Length > 50)
                {
                    errors.Add("Unit cannot exceed 50 characters.");
                }
            }

            // ExpiryDate validation
            if (donation.ExpiryDate == default)
            {
                errors.Add("Expiry date is required.");
            }
            else if (donation.ExpiryDate < DateTime.Today)
            {
                errors.Add("Expiry date must be today or in the future.");
            }
            else if (donation.ExpiryDate > DateTime.Today.AddYears(1))
            {
                errors.Add("Expiry date cannot be more than 1 year in the future.");
            }

            // PickupAddress validation
            if (string.IsNullOrWhiteSpace(donation.PickupAddress))
            {
                errors.Add("Pickup address is required.");
            }
            else if (donation.PickupAddress.Length > 500)
            {
                errors.Add("Pickup address cannot exceed 500 characters.");
            }
            else if (donation.PickupAddress.Length < 5)
            {
                errors.Add("Pickup address must be at least 5 characters long.");
            }

            // ImageUrl validation (optional)
            if (!string.IsNullOrWhiteSpace(donation.ImageUrl))
            {
                if (donation.ImageUrl.Length > 500)
                {
                    errors.Add("Image URL cannot exceed 500 characters.");
                }
                else if (!IsValidUrl(donation.ImageUrl))
                {
                    errors.Add("Image URL must be a valid URL.");
                }
            }

            // Location validation (optional)
            if (!string.IsNullOrWhiteSpace(donation.Location))
            {
                if (donation.Location.Length > 200)
                {
                    errors.Add("Location cannot exceed 200 characters.");
                }
            }

            // Status validation (for updates)
            if (isUpdate)
            {
                var validStatuses = new[] { "Available", "Reserved", "Collected", "Expired" };
                if (!string.IsNullOrWhiteSpace(donation.Status) && !validStatuses.Contains(donation.Status))
                {
                    errors.Add($"Status must be one of: {string.Join(", ", validStatuses)}");
                }
            }

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        /// <summary>
        /// Validates a donation request before creation
        /// </summary>
        public ValidationResult ValidateDonationRequest(DonationRequest request)
        {
            var errors = new List<string>();

            // DonationId validation
            if (request.DonationId <= 0)
            {
                errors.Add("Donation ID is required and must be valid.");
            }

            // NGOId validation
            if (request.NGOId <= 0)
            {
                errors.Add("NGO ID is required and must be valid.");
            }

            // Message validation
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                errors.Add("Message is required.");
            }
            else if (request.Message.Length > 500)
            {
                errors.Add("Message cannot exceed 500 characters.");
            }
            else if (request.Message.Length < 10)
            {
                errors.Add("Message must be at least 10 characters long.");
            }

            // Status validation
            var validStatuses = new[] { "Pending", "Approved", "Rejected", "Completed" };
            if (!string.IsNullOrWhiteSpace(request.Status) && !validStatuses.Contains(request.Status))
            {
                errors.Add($"Status must be one of: {string.Join(", ", validStatuses)}");
            }

            // ResponseMessage validation (optional, for responses)
            if (!string.IsNullOrWhiteSpace(request.ResponseMessage))
            {
                if (request.ResponseMessage.Length > 200)
                {
                    errors.Add("Response message cannot exceed 200 characters.");
                }
            }

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        /// <summary>
        /// Validates that a donation can be requested
        /// </summary>
        public ValidationResult ValidateDonationRequestability(Donation donation, int ngoId)
        {
            var errors = new List<string>();

            if (donation == null)
            {
                errors.Add("Donation not found.");
                return new ValidationResult { IsValid = false, Errors = errors };
            }

            // Check if donation is available
            if (donation.Status != "Available")
            {
                errors.Add($"Donation is not available. Current status: {donation.Status}");
            }

            // Check if donation has expired
            if (donation.ExpiryDate < DateTime.Today)
            {
                errors.Add("Donation has expired.");
            }

            // Check if NGO is trying to request their own donation (shouldn't happen, but validate)
            if (donation.NGOId == ngoId && donation.Status == "Reserved")
            {
                errors.Add("This donation is already reserved for your organization.");
            }

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        /// <summary>
        /// Validates that a donation can be updated
        /// </summary>
        public ValidationResult ValidateDonationUpdate(Donation existingDonation, Donation updatedDonation, string userId, bool isAdmin)
        {
            var errors = new List<string>();

            if (existingDonation == null)
            {
                errors.Add("Donation not found.");
                return new ValidationResult { IsValid = false, Errors = errors };
            }

            // Check ownership (unless admin)
            if (!isAdmin && existingDonation.DonorId != userId)
            {
                errors.Add("You do not have permission to update this donation.");
            }

            // Check if donation can be modified based on status
            if (existingDonation.Status == "Collected")
            {
                errors.Add("Cannot modify a donation that has been collected.");
            }
            else if (existingDonation.Status == "Reserved" && !isAdmin)
            {
                errors.Add("Cannot modify a donation that has been reserved. Please contact support if needed.");
            }

            // Validate the updated donation data
            var donationValidation = ValidateDonation(updatedDonation, isUpdate: true);
            if (!donationValidation.IsValid)
            {
                errors.AddRange(donationValidation.Errors);
            }

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        /// <summary>
        /// Validates that a donation request can be approved/rejected
        /// </summary>
        public ValidationResult ValidateRequestResponse(DonationRequest request, string userId, bool isAdmin)
        {
            var errors = new List<string>();

            if (request == null)
            {
                errors.Add("Donation request not found.");
                return new ValidationResult { IsValid = false, Errors = errors };
            }

            // Check if request is in a valid state for response
            if (request.Status != "Pending")
            {
                errors.Add($"Cannot respond to a request that is not pending. Current status: {request.Status}");
            }

            // Check if donation still exists and is available
            if (request.Donation == null)
            {
                errors.Add("The donation associated with this request no longer exists.");
            }
            else if (request.Donation.Status != "Available")
            {
                errors.Add($"The donation is no longer available. Current status: {request.Donation.Status}");
            }

            // Check ownership (unless admin)
            if (!isAdmin && request.Donation?.DonorId != userId)
            {
                errors.Add("You do not have permission to respond to this request.");
            }

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        /// <summary>
        /// Validates URL format
        /// </summary>
        private bool IsValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            try
            {
                return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
                       (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Result of validation operation
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public string ErrorMessage => string.Join(" ", Errors);
    }
}

