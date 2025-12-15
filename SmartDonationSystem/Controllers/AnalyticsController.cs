using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDonationSystem.Models;
using SmartDonationSystem.Services;
using System.Security.Claims;

namespace SmartDonationSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AnalyticsController : Controller
    {
        private readonly AnalyticsService _analyticsService;

        public AnalyticsController(AnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        // GET: Analytics/Dashboard
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Dashboard()
        {
            var metrics = await _analyticsService.GetDashboardMetricsAsync();
            var regionData = await _analyticsService.GetRegionDistributionAsync();
            var monthlyTrends = await _analyticsService.GetMonthlyTrendsAsync();
            var foodTypeData = await _analyticsService.GetFoodTypeDistributionAsync();
            var topDonors = await _analyticsService.GetTopDonorsAsync();
            var topNGOs = await _analyticsService.GetTopNGOsAsync();
            var systemHealth = await _analyticsService.GetSystemHealthAsync();

            ViewBag.Metrics = metrics;
            ViewBag.RegionData = regionData;
            ViewBag.MonthlyTrends = monthlyTrends;
            ViewBag.FoodTypeData = foodTypeData;
            ViewBag.TopDonors = topDonors;
            ViewBag.TopNGOs = topNGOs;
            ViewBag.SystemHealth = systemHealth;

            return View();
        }

        // GET: Analytics/Metrics
        [HttpGet]
        public async Task<IActionResult> GetMetrics()
        {
            var metrics = await _analyticsService.GetDashboardMetricsAsync();
            return Json(metrics);
        }

        // GET: Analytics/RegionData
        [HttpGet]
        public async Task<IActionResult> GetRegionData()
        {
            var regionData = await _analyticsService.GetRegionDistributionAsync();
            return Json(regionData);
        }

        // GET: Analytics/MonthlyTrends
        [HttpGet]
        public async Task<IActionResult> GetMonthlyTrends()
        {
            var trends = await _analyticsService.GetMonthlyTrendsAsync();
            return Json(trends);
        }

        // GET: Analytics/FoodTypeData
        [HttpGet]
        public async Task<IActionResult> GetFoodTypeData()
        {
            var foodTypeData = await _analyticsService.GetFoodTypeDistributionAsync();
            return Json(foodTypeData);
        }

        // GET: Analytics/TopDonors
        [HttpGet]
        public async Task<IActionResult> GetTopDonors()
        {
            var topDonors = await _analyticsService.GetTopDonorsAsync();
            return Json(topDonors);
        }

        // GET: Analytics/TopNGOs
        [HttpGet]
        public async Task<IActionResult> GetTopNGOs()
        {
            var topNGOs = await _analyticsService.GetTopNGOsAsync();
            return Json(topNGOs);
        }

        // GET: Analytics/SystemHealth
        [HttpGet]
        public async Task<IActionResult> GetSystemHealth()
        {
            var systemHealth = await _analyticsService.GetSystemHealthAsync();
            return Json(systemHealth);
        }

        // POST: Analytics/Export
        [HttpPost]
        public async Task<IActionResult> ExportReport([FromBody] ExportRequest request)
        {
            try
            {
                var report = await _analyticsService.GenerateFullReportAsync();
                
                if (request.Format.ToLower() == "excel")
                {
                    return await ExportToExcel(report, request);
                }
                else if (request.Format.ToLower() == "pdf")
                {
                    return await ExportToPdf(report, request);
                }
                else
                {
                    return BadRequest("Unsupported export format");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generating report: {ex.Message}");
            }
        }

        private async Task<IActionResult> ExportToExcel(AnalyticsReport report, ExportRequest request)
        {
            // For now, return JSON data - in production, you'd use a library like EPPlus or ClosedXML
            var fileName = $"Analytics_Report_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            
            var jsonData = System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            var bytes = System.Text.Encoding.UTF8.GetBytes(jsonData);
            return File(bytes, "application/json", fileName);
        }

        private async Task<IActionResult> ExportToPdf(AnalyticsReport report, ExportRequest request)
        {
            // For now, return JSON data - in production, you'd use a library like iTextSharp or PdfSharp
            var fileName = $"Analytics_Report_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            
            var jsonData = System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            var bytes = System.Text.Encoding.UTF8.GetBytes(jsonData);
            return File(bytes, "application/json", fileName);
        }

        // GET: Analytics/FullReport
        [HttpGet]
        public async Task<IActionResult> GetFullReport()
        {
            var report = await _analyticsService.GenerateFullReportAsync();
            return Json(report);
        }
    }
}
