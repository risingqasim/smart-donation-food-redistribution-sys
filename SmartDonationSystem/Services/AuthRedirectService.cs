using Microsoft.AspNetCore.Identity;
using SmartDonationSystem.Models;

namespace SmartDonationSystem.Services
{
    /// <summary>
    /// Service to determine the appropriate dashboard URL based on user roles
    /// </summary>
    public class AuthRedirectService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthRedirectService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// Gets the dashboard URL for a user based on their roles
        /// Priority: Admin > NGO > Donor > Home
        /// </summary>
        public async Task<string> GetDashboardUrlAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            
            if (roles.Contains("Admin"))
            {
                return "/Admin/Dashboard";
            }
            
            if (roles.Contains("NGO"))
            {
                return "/NGO/Dashboard";
            }
            
            if (roles.Contains("Donor"))
            {
                return "/Donor/Dashboard";
            }

            // Default to home page if no role is assigned
            return "/";
        }

        /// <summary>
        /// Gets the dashboard URL for a user based on their roles (synchronous version using roles list)
        /// </summary>
        public string GetDashboardUrl(IList<string> roles)
        {
            if (roles.Contains("Admin"))
            {
                return "/Admin/Dashboard";
            }
            
            if (roles.Contains("NGO"))
            {
                return "/NGO/Dashboard";
            }
            
            if (roles.Contains("Donor"))
            {
                return "/Donor/Dashboard";
            }

            // Default to home page if no role is assigned
            return "/";
        }
    }
}

