using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDonationSystem.Exceptions;
using SmartDonationSystem.Models;
using SmartDonationSystem.Services;
using System.Security.Claims;

namespace SmartDonationSystem.Controllers
{
    /// <summary>
    /// Controller for donor donation operations
    /// Only accessible to logged-in Donors
    /// </summary>
    [Authorize(Roles = "Donor")]
    public class DonorDonationController : Controller
    {
        private readonly DonationService _donationService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DonorDonationController> _logger;

        public DonorDonationController(
            DonationService donationService, 
            IConfiguration configuration,
            ILogger<DonorDonationController> logger)
        {
            _donationService = donationService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// GET: DonorDonation/AddDonation
        /// Displays the form to add a new donation
        /// </summary>
        [HttpGet]
        public IActionResult AddDonation()
        {
            ViewBag.GoogleMapsApiKey = _configuration["GoogleMaps:ApiKey"];
            return View(new Donation
            {
                ExpiryDate = DateTime.Today.AddDays(1) // Default to tomorrow
            });
        }

        /// <summary>
        /// POST: DonorDonation/AddDonation
        /// Creates a new donation
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDonation([Bind("Title,Description,FoodType,Quantity,Unit,ExpiryDate,PickupAddress,ImageUrl,Location")] Donation donation)
        {
            ViewBag.GoogleMapsApiKey = _configuration["GoogleMaps:ApiKey"];

            if (!ModelState.IsValid)
            {
                return View(donation);
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    ModelState.AddModelError(string.Empty, "User authentication failed. Please log in again.");
                    return View(donation);
                }

                // Create donation using service layer (validation happens in service)
                var createdDonation = await _donationService.CreateDonationAsync(donation, userId);

                TempData["Success"] = "Donation created successfully! NGOs have been notified.";
                return RedirectToAction(nameof(MyDonations));
            }
            catch (DonationValidationException ex)
            {
                // Add validation errors to ModelState
                foreach (var error in ex.ValidationErrors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
                _logger.LogWarning(ex, "Donation validation failed for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                return View(donation);
            }
            catch (DonationPermissionException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                _logger.LogWarning(ex, "Permission denied for donation creation");
                return View(donation);
            }
            catch (DonationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                _logger.LogError(ex, "Error creating donation");
                return View(donation);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating the donation. Please try again.");
                _logger.LogError(ex, "Unexpected error creating donation");
                return View(donation);
            }
        }

        /// <summary>
        /// GET: DonorDonation/MyDonations
        /// Displays all donations created by the logged-in donor
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> MyDonations()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["Error"] = "User authentication failed. Please log in again.";
                    return RedirectToAction("Index", "Home");
                }

                // Get donations using service layer
                var donations = await _donationService.GetDonorDonationsAsync(userId);

                return View(donations);
            }
            catch (DonationException ex)
            {
                TempData["Error"] = ex.Message;
                _logger.LogError(ex, "Error retrieving donations");
                return View(new List<Donation>());
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An unexpected error occurred while loading donations. Please try again.";
                _logger.LogError(ex, "Unexpected error retrieving donations");
                return View(new List<Donation>());
            }
        }
    }
}

