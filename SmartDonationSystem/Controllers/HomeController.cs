using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartDonationSystem.Models;
using SmartDonationSystem.Services;

namespace SmartDonationSystem.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AuthRedirectService _authRedirectService;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(
        ILogger<HomeController> logger,
        AuthRedirectService authRedirectService,
        UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _authRedirectService = authRedirectService;
        _userManager = userManager;
    }

    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Index()
    {
        // Redirect authenticated users to their role-specific dashboard
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var dashboardUrl = await _authRedirectService.GetDashboardUrlAsync(user);
                _logger.LogInformation("Authenticated user {Email} redirected from Home to {DashboardUrl}", 
                    user.Email, dashboardUrl);
                return LocalRedirect(dashboardUrl);
            }
        }

        // Only show home page to unauthenticated users
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
