namespace SmartDonationSystem.Models
{
    public class DashboardMetrics
    {
        public int TotalDonations { get; set; }
        public double TotalFoodSavedKg { get; set; }
        public int ActiveDonors { get; set; }
        public int ActiveNGOs { get; set; }
        public int TotalUsers { get; set; }
        public int CompletedDonations { get; set; }
        public int PendingDonations { get; set; }
        public int ExpiredDonations { get; set; }
        public double AverageDonationSize { get; set; }
        public double CompletionRate { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    public class RegionDistribution
    {
        public string Region { get; set; } = string.Empty;
        public int DonationCount { get; set; }
        public double FoodSavedKg { get; set; }
        public int ActiveDonors { get; set; }
        public int ActiveNGOs { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Percentage { get; set; }
    }

    public class MonthlyTrend
    {
        public string Month { get; set; } = string.Empty;
        public int DonationCount { get; set; }
        public double FoodSavedKg { get; set; }
        public int NewDonors { get; set; }
        public int NewNGOs { get; set; }
        public double CompletionRate { get; set; }
    }

    public class FoodTypeDistribution
    {
        public string FoodType { get; set; } = string.Empty;
        public int Count { get; set; }
        public double TotalQuantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public double Percentage { get; set; }
    }

    public class TopDonors
    {
        public string DonorName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int DonationCount { get; set; }
        public double TotalFoodSaved { get; set; }
        public string LastDonationDate { get; set; } = string.Empty;
        public double AverageDonationSize { get; set; }
    }

    public class TopNGOs
    {
        public string NGOName { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public int DonationCount { get; set; }
        public double TotalFoodReceived { get; set; }
        public string LastActivityDate { get; set; } = string.Empty;
        public double ResponseRate { get; set; }
    }

    public class SystemHealth
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalDonations { get; set; }
        public int ActiveDonations { get; set; }
        public double SystemUptime { get; set; }
        public double AverageResponseTime { get; set; }
        public int PendingRequests { get; set; }
        public int CompletedTransactions { get; set; }
    }

    public class ExportRequest
    {
        public string ReportType { get; set; } = string.Empty; // "donations", "users", "analytics"
        public string Format { get; set; } = string.Empty; // "excel", "pdf"
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Region { get; set; }
        public string? FoodType { get; set; }
        public bool IncludeCharts { get; set; } = true;
    }

    public class AnalyticsReport
    {
        public DashboardMetrics Metrics { get; set; } = new DashboardMetrics();
        public List<RegionDistribution> RegionData { get; set; } = new List<RegionDistribution>();
        public List<MonthlyTrend> MonthlyTrends { get; set; } = new List<MonthlyTrend>();
        public List<FoodTypeDistribution> FoodTypeData { get; set; } = new List<FoodTypeDistribution>();
        public List<TopDonors> TopDonorsList { get; set; } = new List<TopDonors>();
        public List<TopNGOs> TopNGOsList { get; set; } = new List<TopNGOs>();
        public SystemHealth SystemHealth { get; set; } = new SystemHealth();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public string GeneratedBy { get; set; } = string.Empty;
    }
}
