using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDonationSystem.Data;
using SmartDonationSystem.ML;
using SmartDonationSystem.Models;
using SmartDonationSystem.Services;
using System.Security.Claims;

namespace SmartDonationSystem.Controllers
{
    /// <summary>
    /// Controller for AI prediction dashboard
    /// Accessible to NGO and Admin roles
    /// </summary>
    [Authorize(Roles = "NGO,Admin")]
    public class PredictionController : Controller
    {
        private readonly AIPredictionService _predictionService;
        private readonly ApplicationDbContext _context;
        private readonly NGOService _ngoService;
        private readonly ILogger<PredictionController> _logger;

        public PredictionController(
            AIPredictionService predictionService,
            ApplicationDbContext context,
            NGOService ngoService,
            ILogger<PredictionController> logger)
        {
            _predictionService = predictionService;
            _context = context;
            _ngoService = ngoService;
            _logger = logger;
        }

        /// <summary>
        /// GET: Prediction/Dashboard
        /// Displays AI prediction results on dashboard
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var viewModel = new PredictionDashboardViewModel();

                // Get current date and future dates for prediction
                var today = DateTime.UtcNow;
                var nextWeek = today.AddDays(7);
                var nextMonth = today.AddMonths(1);

                // Get food types for predictions
                var foodTypes = await _context.Donations
                    .Where(d => !string.IsNullOrEmpty(d.FoodType))
                    .Select(d => d.FoodType)
                    .Distinct()
                    .ToListAsync();

                // Get predictions for different time periods and food types
                var predictions = new List<FoodDemandPredictionResult>();

                // Predict for today
                var todayPrediction = await _predictionService.PredictFoodDemandAsync(new FoodDemandPredictionRequest
                {
                    Date = today
                });
                todayPrediction.PredictedDate = today;
                predictions.Add(todayPrediction);

                // Predict for next week
                var nextWeekPrediction = await _predictionService.PredictFoodDemandAsync(new FoodDemandPredictionRequest
                {
                    Date = nextWeek
                });
                nextWeekPrediction.PredictedDate = nextWeek;
                predictions.Add(nextWeekPrediction);

                // Predict for next month
                var nextMonthPrediction = await _predictionService.PredictFoodDemandAsync(new FoodDemandPredictionRequest
                {
                    Date = nextMonth
                });
                nextMonthPrediction.PredictedDate = nextMonth;
                predictions.Add(nextMonthPrediction);

                // Get predictions by food type
                var foodTypePredictions = new List<FoodDemandPredictionResult>();
                foreach (var foodType in foodTypes.Take(5)) // Limit to top 5 food types
                {
                    try
                    {
                        var foodPrediction = await _predictionService.PredictFoodDemandAsync(new FoodDemandPredictionRequest
                        {
                            Date = nextWeek,
                            FoodType = foodType
                        });
                        foodPrediction.FoodType = foodType;
                        foodTypePredictions.Add(foodPrediction);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error predicting for food type {FoodType}", foodType);
                    }
                }

                // If user is NGO, get location-specific predictions
                if (User.IsInRole("NGO"))
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var ngo = await _ngoService.GetNGOByUserIdAsync(userId ?? "");
                    
                    if (ngo != null && !string.IsNullOrEmpty(ngo.Location))
                    {
                        try
                        {
                            var locationPrediction = await _predictionService.PredictFoodDemandAsync(new FoodDemandPredictionRequest
                            {
                                Date = nextWeek,
                                Location = ngo.Location
                            });
                            viewModel.LocationSpecificPrediction = locationPrediction;
                            viewModel.NGOLocation = ngo.Location;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error predicting for NGO location");
                        }
                    }
                }

                // Get historical predictions from database
                var historicalPredictions = await _context.Predictions
                    .Where(p => p.PredictionType == "FoodDemand")
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(10)
                    .AsNoTracking()
                    .ToListAsync();

                // Build view model
                viewModel.TodayPrediction = todayPrediction;
                viewModel.NextWeekPrediction = nextWeekPrediction;
                viewModel.NextMonthPrediction = nextMonthPrediction;
                viewModel.FoodTypePredictions = foodTypePredictions;
                viewModel.HistoricalPredictions = historicalPredictions;
                viewModel.AllPredictions = predictions;

                // Calculate summary statistics
                viewModel.HighDemandCount = predictions.Count(p => p.PredictedDemandLevel == "High");
                viewModel.MediumDemandCount = predictions.Count(p => p.PredictedDemandLevel == "Medium");
                viewModel.LowDemandCount = predictions.Count(p => p.PredictedDemandLevel == "Low");
                viewModel.AverageConfidence = predictions.Any() ? predictions.Average(p => p.Confidence) : 0;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading prediction dashboard");
                TempData["Error"] = $"An error occurred while loading predictions: {ex.Message}";
                return View(new PredictionDashboardViewModel());
            }
        }

        /// <summary>
        /// POST: Prediction/GeneratePrediction
        /// Generates a new prediction with custom parameters
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GeneratePrediction([FromForm] FoodDemandPredictionRequest request)
        {
            try
            {
                if (request == null)
                {
                    TempData["Error"] = "Invalid prediction request.";
                    return RedirectToAction(nameof(Dashboard));
                }

                var result = await _predictionService.PredictFoodDemandAsync(request);

                // Save prediction to database
                var prediction = new Prediction
                {
                    PredictionType = "FoodDemand",
                    PredictedValue = result.PredictedDemandLevel,
                    ConfidenceScore = (decimal)result.Confidence,
                    DemandScore = (decimal)result.HighDemandProbability,
                    Metadata = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        FoodType = result.FoodType,
                        HighProbability = result.HighDemandProbability,
                        MediumProbability = result.MediumDemandProbability,
                        LowProbability = result.LowDemandProbability,
                        FeatureImportance = result.FeatureImportance
                    }),
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                };

                _context.Predictions.Add(prediction);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Prediction generated: {result.PredictedDemandLevel} demand (Confidence: {result.Confidence:P0})";
                return RedirectToAction(nameof(Dashboard));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating prediction");
                TempData["Error"] = $"An error occurred while generating prediction: {ex.Message}";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        /// <summary>
        /// GET: Prediction/TrainModel
        /// Trains the ML model (Admin only)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TrainModel()
        {
            try
            {
                await _predictionService.TrainModelAsync();
                TempData["Success"] = "ML model trained successfully.";
                return RedirectToAction(nameof(Dashboard));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training model");
                TempData["Error"] = $"An error occurred while training the model: {ex.Message}";
                return RedirectToAction(nameof(Dashboard));
            }
        }
    }

    /// <summary>
    /// View model for prediction dashboard
    /// </summary>
    public class PredictionDashboardViewModel
    {
        public FoodDemandPredictionResult? TodayPrediction { get; set; }
        public FoodDemandPredictionResult? NextWeekPrediction { get; set; }
        public FoodDemandPredictionResult? NextMonthPrediction { get; set; }
        public List<FoodDemandPredictionResult> FoodTypePredictions { get; set; } = new List<FoodDemandPredictionResult>();
        public List<FoodDemandPredictionResult> AllPredictions { get; set; } = new List<FoodDemandPredictionResult>();
        public List<Prediction> HistoricalPredictions { get; set; } = new List<Prediction>();
        public FoodDemandPredictionResult? LocationSpecificPrediction { get; set; }
        public string? NGOLocation { get; set; }
        public int HighDemandCount { get; set; }
        public int MediumDemandCount { get; set; }
        public int LowDemandCount { get; set; }
        public double AverageConfidence { get; set; }
    }
}

