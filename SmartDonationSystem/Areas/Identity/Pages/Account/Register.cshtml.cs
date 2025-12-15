using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartDonationSystem.Models;
using SmartDonationSystem.Services;

namespace SmartDonationSystem.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AuthRedirectService _authRedirectService;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            AuthRedirectService authRedirectService,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _authRedirectService = authRedirectService;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "First name is required")]
            [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
            [Display(Name = "First Name")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Last name is required")]
            [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
            [Display(Name = "Last Name")]
            public string LastName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email address")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required")]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm Password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Please select a role")]
            [Display(Name = "I want to register as")]
            public string Role { get; set; } = string.Empty;

            [StringLength(500)]
            [Display(Name = "Address")]
            public string? Address { get; set; }

            [StringLength(50)]
            [Display(Name = "City")]
            public string? City { get; set; }

            [StringLength(20)]
            [Display(Name = "Postal Code")]
            public string? PostalCode { get; set; }

            [StringLength(50)]
            [Display(Name = "State/Province")]
            public string? State { get; set; }

            [StringLength(50)]
            [Display(Name = "Country")]
            public string? Country { get; set; }
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                // Validate role
                if (!new[] { "Donor", "NGO", "Admin" }.Contains(Input.Role))
                {
                    ModelState.AddModelError(string.Empty, "Invalid role selected.");
                    return Page();
                }

                var user = new ApplicationUser
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    FirstName = Input.FirstName,
                    LastName = Input.LastName,
                    Address = Input.Address,
                    City = Input.City,
                    PostalCode = Input.PostalCode,
                    State = Input.State,
                    Country = Input.Country
                };

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    // Ensure role exists
                    if (!await _roleManager.RoleExistsAsync(Input.Role))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(Input.Role));
                    }

                    // Add user to role
                    await _userManager.AddToRoleAsync(user, Input.Role);

                    _logger.LogInformation("User created a new account with password.");

                    // Authenticate user - stores session/cookie via ASP.NET Core Identity
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    
                    // POST-REDIRECT-GET Pattern: Always redirect after successful authentication
                    // Never return Page() after authentication to prevent form resubmission
                    var dashboardUrl = await _authRedirectService.GetDashboardUrlAsync(user);
                    // LocalRedirect performs HTTP 302 redirect (GET request)
                    return LocalRedirect(dashboardUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }
    }
}

