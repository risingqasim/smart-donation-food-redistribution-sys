using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartDonationSystem.Models;
using SmartDonationSystem.Services;

namespace SmartDonationSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AuthRedirectService _authRedirectService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            AuthRedirectService authRedirectService,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _authRedirectService = authRedirectService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterRequest model, string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validate role
            if (!new[] { "Donor", "NGO", "Admin" }.Contains(model.Role))
            {
                ModelState.AddModelError(string.Empty, "Invalid role selected. Must be Donor, NGO, or Admin.");
                return View(model);
            }

            // Create user
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Address = model.Address,
                City = model.City,
                PostalCode = model.PostalCode,
                State = model.State,
                Country = model.Country,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password. Email: {Email}", model.Email);

                // Ensure role exists
                if (!await _roleManager.RoleExistsAsync(model.Role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(model.Role));
                    _logger.LogInformation("Role {Role} created.", model.Role);
                }

                // Assign role to user
                var roleResult = await _userManager.AddToRoleAsync(user, model.Role);
                if (roleResult.Succeeded)
                {
                    _logger.LogInformation("User {Email} assigned to role {Role}.", model.Email, model.Role);
                }
                else
                {
                    _logger.LogWarning("Failed to assign role {Role} to user {Email}. Errors: {Errors}", 
                        model.Role, model.Email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    foreach (var error in roleResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, $"Role assignment failed: {error.Description}");
                    }
                    // Return View only on error (not after authentication)
                    return View(model);
                }

                // Authenticate user - stores session/cookie via ASP.NET Core Identity
                await _signInManager.SignInAsync(user, isPersistent: false);

                // POST-REDIRECT-GET Pattern: Always redirect after successful authentication
                // Never return View() after authentication to prevent form resubmission
                var dashboardUrl = await _authRedirectService.GetDashboardUrlAsync(user);
                // LocalRedirect performs HTTP 302 redirect (GET request)
                return LocalRedirect(dashboardUrl);
            }

            // If user creation failed, add errors to ModelState
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult Login(string? returnUrl = null)
        {
            _logger.LogInformation("GET Login action called. ReturnUrl: {ReturnUrl}, Request Path: {Path}, Method: {Method}", 
                returnUrl, HttpContext.Request.Path, HttpContext.Request.Method);
            ViewData["ReturnUrl"] = returnUrl;
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            return View("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Login(LoginRequest model, string? returnUrl = null)
        {
            _logger.LogInformation("POST Login action called. Email: {Email}, ReturnUrl: {ReturnUrl}, Request Path: {Path}, Method: {Method}", 
                model?.Email, returnUrl, HttpContext.Request.Path, HttpContext.Request.Method);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Login POST: ModelState invalid for Email: {Email}", model?.Email);
                return View("Login", model);
            }

            if (model == null)
            {
                _logger.LogWarning("Login POST: Model is null");
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View("Login");
            }

            // Find user by email
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View("Login", model);
            }

            // Attempt to sign in
            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                // Authentication cookie is set via SignInManager.PasswordSignInAsync above
                // ASP.NET Core Identity automatically creates authentication cookie/session
                _logger.LogInformation("User logged in. Email: {Email}", model.Email);

                // POST-REDIRECT-GET Pattern: Always redirect after successful authentication
                // Never return View() after POST to prevent form resubmission
                // Authentication cookie is already set by SignInManager.PasswordSignInAsync above
                
                // If returnUrl is provided (not default) and is a local URL, redirect to it
                // This handles cases where user was redirected to login from a protected page
                if (!string.IsNullOrEmpty(returnUrl) && 
                    returnUrl != "/" && 
                    returnUrl != Url.Content("~/") && 
                    Url.IsLocalUrl(returnUrl))
                {
                    _logger.LogInformation("Redirecting to returnUrl: {ReturnUrl}", returnUrl);
                    return LocalRedirect(returnUrl);
                }

                // Redirect to role-specific dashboard using AuthRedirectService (consistent with Register)
                // Admin → /Admin/Dashboard
                // NGO → /NGO/Dashboard  
                // Donor → /Donor/Dashboard
                var dashboardUrl = await _authRedirectService.GetDashboardUrlAsync(user);
                _logger.LogInformation("Redirecting to dashboard: {DashboardUrl} for user: {Email}", dashboardUrl, model.Email);
                return LocalRedirect(dashboardUrl);
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out. Email: {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "Your account has been locked out. Please try again later.");
                return View("Login", model);
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View("Login", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}

