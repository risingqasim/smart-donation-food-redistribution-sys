using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.EntityFrameworkCore;
using SmartDonationSystem.Data;
using SmartDonationSystem.Models;
using SmartDonationSystem.ML;

namespace SmartDonationSystem.Services
{
    /// <summary>
    /// AI Prediction Service using ML.NET for food demand level prediction
    /// </summary>
    public class AIPredictionService
    {
        private readonly MLContext _mlContext;
        private readonly ApplicationDbContext _context;
        private ITransformer? _model;
        private readonly string _modelPath;
        private readonly ILogger<AIPredictionService> _logger;

        // Food type mapping
        private static readonly Dictionary<string, float> FoodTypeMap = new()
        {
            { "Vegetables", 1.0f },
            { "Fruits", 2.0f },
            { "Grains", 3.0f },
            { "Dairy", 4.0f },
            { "Meat", 5.0f },
            { "Bakery", 6.0f },
            { "Beverages", 7.0f },
            { "Other", 8.0f }
        };

        public AIPredictionService(
            ApplicationDbContext context,
            ILogger<AIPredictionService> logger)
        {
            _mlContext = new MLContext(seed: 1);
            _context = context;
            _logger = logger;
            _modelPath = Path.Combine(Directory.GetCurrentDirectory(), "MLModels", "fooddemand_model.zip");
        }

        /// <summary>
        /// Trains the ML model using historical donation data
        /// </summary>
        public async Task TrainModelAsync()
        {
            try
            {
                _logger.LogInformation("Starting model training...");

                // Load historical donation data
                var trainingData = await LoadHistoricalDonationDataAsync();

                if (!trainingData.Any())
                {
                    _logger.LogWarning("No historical data found. Generating synthetic training data...");
                    trainingData = GenerateSyntheticTrainingData();
                }

                _logger.LogInformation($"Loaded {trainingData.Count} training samples.");

                // Convert to IDataView
                var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

                // Define the ML pipeline
                var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("DemandLevel", "DemandLevel")
                    .Append(_mlContext.Transforms.Concatenate("Features",
                        "Month", "DayOfWeek", "Season", "FoodTypeIndex",
                        "AverageQuantity", "DonationFrequency", "RequestFrequency",
                        "CompletionRate", "LocationActivity"))
                    .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
                    .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                        labelColumnName: "DemandLevel",
                        featureColumnName: "Features"))
                    .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

                // Train the model
                _model = pipeline.Fit(dataView);

                // Save the model
                var directory = Path.GetDirectoryName(_modelPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                _mlContext.Model.Save(_model, dataView.Schema, _modelPath);

                _logger.LogInformation("Model trained and saved successfully at {ModelPath}", _modelPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training model");
                throw;
            }
        }

        /// <summary>
        /// Predicts food demand level based on input features
        /// </summary>
        public async Task<FoodDemandPredictionResult> PredictFoodDemandAsync(FoodDemandPredictionRequest request)
        {
            try
            {
                // Load model if not already loaded
                if (_model == null)
                {
                    if (File.Exists(_modelPath))
                    {
                        _model = _mlContext.Model.Load(_modelPath, out var modelSchema);
                        _logger.LogInformation("Model loaded from {ModelPath}", _modelPath);
                    }
                    else
                    {
                        _logger.LogWarning("Model not found. Training new model...");
                        await TrainModelAsync();
                    }
                }

                // Calculate features from historical data
                var features = await CalculateFeaturesAsync(request);

                // Create prediction input
                var predictionInput = new FoodDemandData
                {
                    Month = features.Month,
                    DayOfWeek = features.DayOfWeek,
                    Season = features.Season,
                    FoodTypeIndex = features.FoodTypeIndex,
                    AverageQuantity = features.AverageQuantity,
                    DonationFrequency = features.DonationFrequency,
                    RequestFrequency = features.RequestFrequency,
                    CompletionRate = features.CompletionRate,
                    LocationActivity = features.LocationActivity
                };

                // Make prediction
                var predictionEngine = _mlContext.Model.CreatePredictionEngine<FoodDemandData, FoodDemandPrediction>(_model!);
                var prediction = predictionEngine.Predict(predictionInput);

                // Build result
                var result = new FoodDemandPredictionResult
                {
                    PredictedDemandLevel = prediction.PredictedDemandLevel,
                    Confidence = prediction.Confidence,
                    HighDemandProbability = prediction.HighDemandScore,
                    MediumDemandProbability = prediction.MediumDemandScore,
                    LowDemandProbability = prediction.LowDemandScore,
                    PredictedDate = request.Date ?? DateTime.UtcNow,
                    FoodType = request.FoodType,
                    FeatureImportance = new Dictionary<string, float>
                    {
                        { "Month", features.Month },
                        { "Season", features.Season },
                        { "FoodTypeIndex", features.FoodTypeIndex },
                        { "DonationFrequency", features.DonationFrequency },
                        { "RequestFrequency", features.RequestFrequency }
                    }
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error predicting food demand");
                throw;
            }
        }

        /// <summary>
        /// Loads historical donation data from database
        /// </summary>
        private async Task<List<FoodDemandData>> LoadHistoricalDonationDataAsync()
        {
            var trainingData = new List<FoodDemandData>();

            try
            {
                // Get historical donations with related data
                var donations = await _context.Donations
                    .Include(d => d.Donor)
                    .Include(d => d.NGO)
                    .Include(d => d.DonationRequests)
                    .AsNoTracking()
                    .ToListAsync();

                if (!donations.Any())
                {
                    return trainingData;
                }

                // Group donations by time periods (e.g., weekly or monthly)
                var groupedDonations = donations
                    .GroupBy(d => new
                    {
                        Year = d.CreatedAt.Year,
                        Month = d.CreatedAt.Month,
                        Week = GetWeekOfMonth(d.CreatedAt)
                    })
                    .ToList();

                foreach (var group in groupedDonations)
                {
                    var groupDonations = group.ToList();
                    var date = groupDonations.First().CreatedAt;

                    // Calculate features for this time period
                    var features = CalculateFeaturesForPeriod(groupDonations, date);

                    // Determine demand level based on historical patterns
                    var demandLevel = DetermineDemandLevel(groupDonations);

                    trainingData.Add(new FoodDemandData
                    {
                        Month = date.Month,
                        DayOfWeek = (float)date.DayOfWeek,
                        Season = GetSeason(date),
                        FoodTypeIndex = GetAverageFoodTypeIndex(groupDonations),
                        AverageQuantity = features.AverageQuantity,
                        DonationFrequency = features.DonationFrequency,
                        RequestFrequency = features.RequestFrequency,
                        CompletionRate = features.CompletionRate,
                        LocationActivity = features.LocationActivity,
                        DemandLevel = demandLevel
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading historical donation data");
            }

            return trainingData;
        }

        /// <summary>
        /// Calculates features for prediction request
        /// </summary>
        private async Task<FoodDemandFeatures> CalculateFeaturesAsync(FoodDemandPredictionRequest request)
        {
            var date = request.Date ?? DateTime.UtcNow;
            var cutoffDate = date.AddMonths(-3); // Look at last 3 months

            // Get recent donations
            var recentDonations = await _context.Donations
                .Include(d => d.DonationRequests)
                .Where(d => d.CreatedAt >= cutoffDate)
                .AsNoTracking()
                .ToListAsync();

            // Filter by food type if specified
            if (!string.IsNullOrEmpty(request.FoodType))
            {
                recentDonations = recentDonations
                    .Where(d => d.FoodType.Equals(request.FoodType, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var features = CalculateFeaturesForPeriod(recentDonations, date);

            return features;
        }

        /// <summary>
        /// Calculates features for a period of donations
        /// </summary>
        private FoodDemandFeatures CalculateFeaturesForPeriod(List<Donation> donations, DateTime date)
        {
            if (!donations.Any())
            {
                return new FoodDemandFeatures();
            }

            var requests = donations.SelectMany(d => d.DonationRequests).ToList();
            var completedRequests = requests.Count(r => r.Status == "Completed");
            var totalRequests = requests.Count;

            return new FoodDemandFeatures
            {
                Month = date.Month,
                DayOfWeek = (float)date.DayOfWeek,
                Season = GetSeason(date),
                FoodTypeIndex = GetAverageFoodTypeIndex(donations),
                AverageQuantity = (float)donations.Average(d => d.Quantity),
                DonationFrequency = donations.Count / 30.0f, // Donations per day (normalized)
                RequestFrequency = totalRequests / 30.0f, // Requests per day (normalized)
                CompletionRate = totalRequests > 0 ? (float)completedRequests / totalRequests : 0.5f,
                LocationActivity = CalculateLocationActivity(donations)
            };
        }

        /// <summary>
        /// Determines demand level based on donation patterns
        /// </summary>
        private string DetermineDemandLevel(List<Donation> donations)
        {
            var requestCount = donations.Sum(d => d.DonationRequests.Count);
            var completedCount = donations.Sum(d => d.DonationRequests.Count(r => r.Status == "Completed"));
            var avgQuantity = donations.Average(d => d.Quantity);

            var score = (donations.Count * 0.3f) +
                       (requestCount * 0.3f) +
                       (completedCount * 0.2f) +
                       ((float)avgQuantity / 100.0f * 0.2f);

            if (score > 0.7) return "High";
            else if (score > 0.4) return "Medium";
            else return "Low";
        }

        private float GetSeason(DateTime date)
        {
            return date.Month switch
            {
                >= 3 and <= 5 => 1.0f, // Spring
                >= 6 and <= 8 => 2.0f, // Summer
                >= 9 and <= 11 => 3.0f, // Fall
                _ => 4.0f // Winter
            };
        }

        private float GetAverageFoodTypeIndex(List<Donation> donations)
        {
            if (!donations.Any()) return 0;

            var avgIndex = donations
                .Select(d => FoodTypeMap.GetValueOrDefault(d.FoodType, 0))
                .Where(i => i > 0)
                .DefaultIfEmpty(0)
                .Average();

            return (float)avgIndex;
        }

        private float CalculateLocationActivity(List<Donation> donations)
        {
            if (!donations.Any()) return 0;

            var uniqueLocations = donations
                .Where(d => !string.IsNullOrEmpty(d.Location))
                .Select(d => d.Location)
                .Distinct()
                .Count();

            return Math.Min(uniqueLocations / 10.0f, 1.0f);
        }

        private int GetWeekOfMonth(DateTime date)
        {
            return (date.Day - 1) / 7 + 1;
        }

        /// <summary>
        /// Generates synthetic training data when historical data is insufficient
        /// </summary>
        private List<FoodDemandData> GenerateSyntheticTrainingData()
        {
            var trainingData = new List<FoodDemandData>();
            var random = new Random(42);

            for (int i = 0; i < 500; i++)
            {
                var month = random.Next(1, 13);
                var dayOfWeek = random.Next(0, 7);
                var season = GetSeason(new DateTime(2024, month, 1));
                var foodTypeIndex = random.Next(1, 9);
                var avgQuantity = random.Next(10, 200);
                var donationFreq = (float)random.NextDouble() * 2;
                var requestFreq = (float)random.NextDouble() * 2;
                var completionRate = (float)random.NextDouble();
                var locationActivity = (float)random.NextDouble();

                var score = (donationFreq * 0.3f) + (requestFreq * 0.3f) +
                           (completionRate * 0.2f) + (locationActivity * 0.2f);

                var demandLevel = score > 0.7 ? "High" : score > 0.4 ? "Medium" : "Low";

                trainingData.Add(new FoodDemandData
                {
                    Month = month,
                    DayOfWeek = dayOfWeek,
                    Season = season,
                    FoodTypeIndex = foodTypeIndex,
                    AverageQuantity = avgQuantity,
                    DonationFrequency = donationFreq,
                    RequestFrequency = requestFreq,
                    CompletionRate = completionRate,
                    LocationActivity = locationActivity,
                    DemandLevel = demandLevel
                });
            }

            return trainingData;
        }
    }

    /// <summary>
    /// Features extracted from donation data
    /// </summary>
    public class FoodDemandFeatures
    {
        public float Month { get; set; }
        public float DayOfWeek { get; set; }
        public float Season { get; set; }
        public float FoodTypeIndex { get; set; }
        public float AverageQuantity { get; set; }
        public float DonationFrequency { get; set; }
        public float RequestFrequency { get; set; }
        public float CompletionRate { get; set; }
        public float LocationActivity { get; set; }
    }
}

