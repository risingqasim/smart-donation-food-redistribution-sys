using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using SmartDonationSystem.Data;
using SmartDonationSystem.Hubs;
using SmartDonationSystem.Models;
using SmartDonationSystem.Services;
using System.Text;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
// builder.Services.AddDatabaseDeveloperPageExceptionFilter(); // Removed - not available in .NET 9

// Configure Identity for MVC
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => 
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure Cookie Authentication for MVC
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Configure JWT for API
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddScoped<JwtService>(provider => 
    new JwtService(provider.GetRequiredService<IOptions<JwtSettings>>().Value));
builder.Services.AddHttpClient<GoogleMapsService>();
builder.Services.AddScoped<GoogleMapsService>();
builder.Services.AddScoped<MLService>();
builder.Services.AddScoped<AIPredictionService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<AnalyticsService>();
builder.Services.AddScoped<AuthRedirectService>();
builder.Services.AddScoped<ValidationService>();
builder.Services.AddScoped<DonationService>();
builder.Services.AddScoped<NGOService>();
builder.Services.AddSignalR();

// Configure authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("NGOOnly", policy => policy.RequireRole("NGO"));
    options.AddPolicy("DonorOnly", policy => policy.RequireRole("Donor"));
    options.AddPolicy("AdminOrNGO", policy => policy.RequireRole("Admin", "NGO"));
    options.AddPolicy("AdminOrDonor", policy => policy.RequireRole("Admin", "Donor"));
});

// Configure Authentication schemes (Cookie for MVC, JWT for API)
builder.Services.AddAuthentication(options =>
{
    // Default to cookies for MVC web requests
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings?.Issuer,
        ValidAudience = jwtSettings?.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings?.SecretKey ?? ""))
    };
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.UseMigrationsEndPoint(); // Removed - not available in .NET 9
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Configure static files
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Map MVC controllers with default route
// API controllers use attribute routing ([Route("api/[controller]")]) and will be matched automatically
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map Razor Pages (Identity pages are in Areas/Identity/Pages)
app.MapRazorPages();

// Map SignalR hub
app.MapHub<NotificationHub>("/notificationHub");

// Seed database in background (non-blocking)
_ = Task.Run(async () =>
{
    try
    {
        // Wait a bit for the app to start
        await Task.Delay(1000);
        using (var scope = app.Services.CreateScope())
        {
            await DatabaseSeeder.SeedAsync(scope.ServiceProvider);
        }
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error during background database seeding");
    }
});

// Display startup message
Console.WriteLine();
Console.WriteLine("==========================================");
Console.WriteLine("   Smart Donation System Starting...");
Console.WriteLine("==========================================");
Console.WriteLine();

// Use ApplicationStarted event to display URLs
app.Lifetime.ApplicationStarted.Register(() =>
{
    var addresses = app.Urls;
    Console.WriteLine("   Application is running at:");
    foreach (var address in addresses)
    {
        Console.WriteLine($"   {address}");
    }
    Console.WriteLine();
    Console.WriteLine("   Press Ctrl+C to stop the application");
    Console.WriteLine("==========================================");
    Console.WriteLine();
});

app.Run();
