using Microsoft.EntityFrameworkCore;
using SmartDonationSystem.Data;
using SmartDonationSystem.Models;
using System.Security.Claims;

namespace SmartDonationSystem.Services
{
    public class AnalyticsService
    {
        private readonly ApplicationDbContext _context;

        public AnalyticsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardMetrics> GetDashboardMetricsAsync()
        {
            var donations = await _context.Donations
                .Include(d => d.Donor)
                .Include(d => d.NGO)
                .AsNoTracking()
                .ToListAsync();

            var users = await _context.Users
                .AsNoTracking()
                .ToListAsync();

            var ngos = await _context.NGOs
                .AsNoTracking()
                .ToListAsync();

            var totalDonations = donations.Count;
            var totalFoodSaved = donations.Sum(d => d.Quantity);
            var activeDonors = donations.Select(d => d.DonorId).Distinct().Count();
            var activeNGOs = donations.Where(d => d.NGOId.HasValue).Select(d => d.NGOId).Distinct().Count();
            var completedDonations = donations.Count(d => d.Status == "Collected");
            var pendingDonations = donations.Count(d => d.Status == "Available" || d.Status == "Reserved");
            var expiredDonations = donations.Count(d => d.Status == "Expired");
            var averageDonationSize = totalDonations > 0 ? (double)totalFoodSaved / totalDonations : 0;
            var completionRate = totalDonations > 0 ? (double)completedDonations / totalDonations * 100 : 0;

            return new DashboardMetrics
            {
                TotalDonations = totalDonations,
                TotalFoodSavedKg = totalFoodSaved,
                ActiveDonors = activeDonors,
                ActiveNGOs = activeNGOs,
                TotalUsers = users.Count,
                CompletedDonations = completedDonations,
                PendingDonations = pendingDonations,
                ExpiredDonations = expiredDonations,
                AverageDonationSize = Math.Round(averageDonationSize, 2),
                CompletionRate = Math.Round(completionRate, 2),
                LastUpdated = DateTime.UtcNow
            };
        }

        public async Task<List<RegionDistribution>> GetRegionDistributionAsync()
        {
            var donations = await _context.Donations
                .Include(d => d.Donor)
                .Include(d => d.NGO)
                .AsNoTracking()
                .ToListAsync();

            var regionData = donations
                .Where(d => !string.IsNullOrEmpty(d.Location))
                .GroupBy(d => d.Location)
                .Select(g => new RegionDistribution
                {
                    Region = g.Key ?? "Unknown",
                    DonationCount = g.Count(),
                    FoodSavedKg = g.Sum(d => d.Quantity),
                    ActiveDonors = g.Select(d => d.DonorId).Distinct().Count(),
                    ActiveNGOs = g.Where(d => d.NGOId.HasValue).Select(d => d.NGOId).Distinct().Count(),
                    Latitude = g.First().Donor?.Latitude ?? 0,
                    Longitude = g.First().Donor?.Longitude ?? 0
                })
                .OrderByDescending(r => r.DonationCount)
                .ToList();

            var totalDonations = regionData.Sum(r => r.DonationCount);
            foreach (var region in regionData)
            {
                region.Percentage = totalDonations > 0 ? Math.Round((double)region.DonationCount / totalDonations * 100, 2) : 0;
            }

            return regionData;
        }

        public async Task<List<MonthlyTrend>> GetMonthlyTrendsAsync(int months = 12)
        {
            var startDate = DateTime.UtcNow.AddMonths(-months);
            var donations = await _context.Donations
                .Where(d => d.CreatedAt >= startDate)
                .Include(d => d.Donor)
                .Include(d => d.NGO)
                .AsNoTracking()
                .ToListAsync();

            var users = await _context.Users
                .Where(u => u.CreatedAt >= startDate)
                .AsNoTracking()
                .ToListAsync();

            var ngos = await _context.NGOs
                .Where(n => n.CreatedAt >= startDate)
                .AsNoTracking()
                .ToListAsync();

            var monthlyTrends = new List<MonthlyTrend>();
            for (int i = months - 1; i >= 0; i--)
            {
                var monthStart = DateTime.UtcNow.AddMonths(-i).Date;
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                var monthName = monthStart.ToString("MMM yyyy");

                var monthDonations = donations.Where(d => d.CreatedAt >= monthStart && d.CreatedAt <= monthEnd).ToList();
                var monthUsers = users.Where(u => u.CreatedAt >= monthStart && u.CreatedAt <= monthEnd).ToList();
                var monthNGOs = ngos.Where(n => n.CreatedAt >= monthStart && n.CreatedAt <= monthEnd).ToList();

                var donorUsers = monthUsers.Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && 
                    _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Donor"))).Count();
                var ngoUsers = monthUsers.Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && 
                    _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "NGO"))).Count();

                var completedDonations = monthDonations.Count(d => d.Status == "Collected");
                var completionRate = monthDonations.Count > 0 ? (double)completedDonations / monthDonations.Count * 100 : 0;

                monthlyTrends.Add(new MonthlyTrend
                {
                    Month = monthName,
                    DonationCount = monthDonations.Count,
                    FoodSavedKg = monthDonations.Sum(d => d.Quantity),
                    NewDonors = donorUsers,
                    NewNGOs = ngoUsers,
                    CompletionRate = Math.Round(completionRate, 2)
                });
            }

            return monthlyTrends;
        }

        public async Task<List<FoodTypeDistribution>> GetFoodTypeDistributionAsync()
        {
            var donations = await _context.Donations
                .AsNoTracking()
                .ToListAsync();

            var foodTypeData = donations
                .GroupBy(d => d.FoodType)
                .Select(g => new FoodTypeDistribution
                {
                    FoodType = g.Key,
                    Count = g.Count(),
                    TotalQuantity = g.Sum(d => d.Quantity),
                    Unit = g.First().Unit ?? "kg"
                })
                .OrderByDescending(f => f.Count)
                .ToList();

            var totalDonations = foodTypeData.Sum(f => f.Count);
            foreach (var foodType in foodTypeData)
            {
                foodType.Percentage = totalDonations > 0 ? Math.Round((double)foodType.Count / totalDonations * 100, 2) : 0;
            }

            return foodTypeData;
        }

        public async Task<List<TopDonors>> GetTopDonorsAsync(int count = 10)
        {
            var donations = await _context.Donations
                .Include(d => d.Donor)
                .AsNoTracking()
                .ToListAsync();

            var topDonors = donations
                .GroupBy(d => new { d.DonorId, d.Donor })
                .Select(g => new TopDonors
                {
                    DonorName = $"{g.First().Donor?.FirstName} {g.First().Donor?.LastName}",
                    Email = g.First().Donor?.Email ?? "",
                    DonationCount = g.Count(),
                    TotalFoodSaved = g.Sum(d => d.Quantity),
                    LastDonationDate = g.Max(d => d.CreatedAt).ToString("MMM dd, yyyy"),
                    AverageDonationSize = Math.Round(g.Average(d => d.Quantity), 2)
                })
                .OrderByDescending(d => d.DonationCount)
                .Take(count)
                .ToList();

            return topDonors;
        }

        public async Task<List<TopNGOs>> GetTopNGOsAsync(int count = 10)
        {
            var donations = await _context.Donations
                .Include(d => d.NGO)
                .AsNoTracking()
                .ToListAsync();

            var donationRequests = await _context.DonationRequests
                .Include(dr => dr.NGO)
                .AsNoTracking()
                .ToListAsync();

            var topNGOs = donations
                .Where(d => d.NGOId.HasValue)
                .GroupBy(d => new { d.NGOId, d.NGO })
                .Select(g => new TopNGOs
                {
                    NGOName = g.First().NGO?.Name ?? "",
                    Contact = g.First().NGO?.Contact ?? "",
                    DonationCount = g.Count(),
                    TotalFoodReceived = g.Sum(d => d.Quantity),
                    LastActivityDate = g.Max(d => d.CreatedAt).ToString("MMM dd, yyyy"),
                    ResponseRate = 85.0 // Placeholder - would need more complex calculation
                })
                .OrderByDescending(n => n.DonationCount)
                .Take(count)
                .ToList();

            return topNGOs;
        }

        public async Task<SystemHealth> GetSystemHealthAsync()
        {
            var users = await _context.Users.AsNoTracking().ToListAsync();
            var donations = await _context.Donations.AsNoTracking().ToListAsync();
            var requests = await _context.DonationRequests.AsNoTracking().ToListAsync();

            var activeUsers = users.Count(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-30));
            var activeDonations = donations.Count(d => d.CreatedAt >= DateTime.UtcNow.AddDays(-30));
            var pendingRequests = requests.Count(r => r.Status == "Pending");
            var completedTransactions = donations.Count(d => d.Status == "Collected");

            return new SystemHealth
            {
                TotalUsers = users.Count,
                ActiveUsers = activeUsers,
                TotalDonations = donations.Count,
                ActiveDonations = activeDonations,
                SystemUptime = 99.9, // Placeholder
                AverageResponseTime = 2.5, // Placeholder
                PendingRequests = pendingRequests,
                CompletedTransactions = completedTransactions
            };
        }

        public async Task<AnalyticsReport> GenerateFullReportAsync()
        {
            var metrics = await GetDashboardMetricsAsync();
            var regionData = await GetRegionDistributionAsync();
            var monthlyTrends = await GetMonthlyTrendsAsync();
            var foodTypeData = await GetFoodTypeDistributionAsync();
            var topDonors = await GetTopDonorsAsync();
            var topNGOs = await GetTopNGOsAsync();
            var systemHealth = await GetSystemHealthAsync();

            return new AnalyticsReport
            {
                Metrics = metrics,
                RegionData = regionData,
                MonthlyTrends = monthlyTrends,
                FoodTypeData = foodTypeData,
                TopDonorsList = topDonors,
                TopNGOsList = topNGOs,
                SystemHealth = systemHealth,
                GeneratedAt = DateTime.UtcNow,
                GeneratedBy = "System"
            };
        }
    }
}
