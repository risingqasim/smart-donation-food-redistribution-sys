using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartDonationSystem.Models;
using SmartDonationSystem.Services;
using System.Security.Claims;

namespace SmartDonationSystem.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly JwtService _jwtService;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            JwtService jwtService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _jwtService = jwtService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if role is valid
            if (!new[] { "Donor", "NGO", "Admin" }.Contains(model.Role))
            {
                return BadRequest("Invalid role. Must be Donor, NGO, or Admin.");
            }

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
                Country = model.Country
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Add user to role
                await _userManager.AddToRoleAsync(user, model.Role);

                // Generate JWT token
                var roles = await _userManager.GetRolesAsync(user);
                var token = _jwtService.GenerateToken(user, roles);

                return Ok(new AuthResponse
                {
                    Token = token,
                    UserId = user.Id,
                    Email = user.Email!,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Roles = roles.ToList(),
                    ExpiresAt = DateTime.UtcNow.AddMinutes(60)
                });
            }

            return BadRequest(result.Errors);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Unauthorized("Invalid email or password.");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!result.Succeeded)
            {
                return Unauthorized("Invalid email or password.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtService.GenerateToken(user, roles);

            return Ok(new AuthResponse
            {
                Token = token,
                UserId = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles.ToList(),
                ExpiresAt = DateTime.UtcNow.AddMinutes(60)
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok("Logged out successfully.");
        }

        [HttpGet("profile")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Address = user.Address,
                City = user.City,
                PostalCode = user.PostalCode,
                State = user.State,
                Country = user.Country,
                Roles = roles,
                CreatedAt = user.CreatedAt
            });
        }
    }
}
