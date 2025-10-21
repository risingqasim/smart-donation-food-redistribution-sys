using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartDonationSystem.Data;
using SmartDonationSystem.Models;

namespace SmartDonationSystem.Services
{
    public class DatabaseSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Create roles
            string[] roles = { "Donor", "NGO", "Admin" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Create admin user
            var adminEmail = "admin@smartdonation.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Administrator",
                    EmailConfirmed = true,
                    Address = "123 Admin Street",
                    City = "Admin City",
                    State = "AC",
                    Country = "Admin Country"
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Create sample donor user
            var donorEmail = "donor@example.com";
            var donorUser = await userManager.FindByEmailAsync(donorEmail);
            if (donorUser == null)
            {
                donorUser = new ApplicationUser
                {
                    UserName = donorEmail,
                    Email = donorEmail,
                    FirstName = "John",
                    LastName = "Donor",
                    EmailConfirmed = true,
                    Address = "456 Donor Avenue",
                    City = "Donor City",
                    State = "DC",
                    Country = "Donor Country"
                };

                var result = await userManager.CreateAsync(donorUser, "Donor@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(donorUser, "Donor");
                }
            }

            // Create sample NGO user
            var ngoEmail = "ngo@example.com";
            var ngoUser = await userManager.FindByEmailAsync(ngoEmail);
            if (ngoUser == null)
            {
                ngoUser = new ApplicationUser
                {
                    UserName = ngoEmail,
                    Email = ngoEmail,
                    FirstName = "Jane",
                    LastName = "NGO Manager",
                    EmailConfirmed = true,
                    Address = "789 NGO Boulevard",
                    City = "NGO City",
                    State = "NC",
                    Country = "NGO Country"
                };

                var result = await userManager.CreateAsync(ngoUser, "NGO@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(ngoUser, "NGO");

                    // Create NGO profile
                    var ngo = new NGO
                    {
                        Name = "Food Bank Central",
                        Location = "789 NGO Boulevard, NGO City, NC",
                        Contact = "contact@foodbank.org",
                        Capacity = 1000,
                        Description = "A non-profit organization dedicated to fighting hunger in our community",
                        Website = "https://foodbank.org",
                        RegistrationNumber = "NGO-12345",
                        UserId = ngoUser.Id
                    };

                    context.NGOs.Add(ngo);
                    await context.SaveChangesAsync();
                }
            }

            // Create sample donations if they don't exist
            if (!context.Donations.Any())
            {
                var sampleDonations = new List<Donation>
                {
                    new Donation
                    {
                        Title = "Fresh Vegetables",
                        Description = "Organic vegetables from local farm, perfect for families in need",
                        FoodType = "Vegetables",
                        Quantity = 50,
                        Unit = "kg",
                        ExpiryDate = DateTime.Now.AddDays(3),
                        PickupAddress = "123 Farm Road, Green Valley",
                        Location = "Green Valley",
                        Status = "Available",
                        DonorId = donorUser.Id,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Donation
                    {
                        Title = "Bread and Pastries",
                        Description = "Fresh bread and pastries from local bakery",
                        FoodType = "Bakery",
                        Quantity = 25,
                        Unit = "pieces",
                        ExpiryDate = DateTime.Now.AddDays(1),
                        PickupAddress = "456 Bakery Street, Downtown",
                        Location = "Downtown",
                        Status = "Available",
                        DonorId = donorUser.Id,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Donation
                    {
                        Title = "Canned Goods",
                        Description = "Various canned vegetables and fruits",
                        FoodType = "Other",
                        Quantity = 100,
                        Unit = "cans",
                        ExpiryDate = DateTime.Now.AddMonths(6),
                        PickupAddress = "789 Storage Lane, Industrial District",
                        Location = "Industrial District",
                        Status = "Available",
                        DonorId = donorUser.Id,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                context.Donations.AddRange(sampleDonations);
                await context.SaveChangesAsync();
            }
        }
    }
}
