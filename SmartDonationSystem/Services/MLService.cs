using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.EntityFrameworkCore;
using SmartDonationSystem.Data;
using SmartDonationSystem.Models;
using SmartDonationSystem.ML;
using System.Text.Json;

namespace SmartDonationSystem.Services
{
    public class MLService
    {
        private readonly MLContext _mlContext;
        private readonly ApplicationDbContext _context;
        private ITransformer? _model;
        private readonly string _modelPath;

        public MLService(ApplicationDbContext context)
        {
            _mlContext = new MLContext(seed: 1);
            _context = context;
            _modelPath = Path.Combine(Directory.GetCurrentDirectory(), "Models", "ngodemand_model.zip");
        }

        public async Task TrainModelAsync()
        {
            try
            {
                // Generate training data from historical data
                var trainingData = await GenerateTrainingDataAsync();
                
                if (!trainingData.Any())
                {
                    // Create synthetic training data if no historical data exists
                    trainingData = GenerateSyntheticTrainingData();
                }

                // Convert to IDataView
                var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

                // Define the pipeline
                var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("DemandLevel", "DemandLevel")
                    .Append(_mlContext.Transforms.Concatenate("Features", 
                        "PastDonationsCount", "PastDonationsTotalQuantity", "NGOCapacity", 
                        "LocationActivityScore", "DistanceFromDonor", "TimeOfDay", 
                        "DayOfWeek", "Season", "FoodTypeMatch", "ResponseTime", "CompletionRate"))
                    .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy())
                    .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

                // Train the model
                _model = pipeline.Fit(dataView);

                // Save the model
                Directory.CreateDirectory(Path.GetDirectoryName(_modelPath)!);
                _mlContext.Model.Save(_model, dataView.Schema, _modelPath);

                Console.WriteLine("ML Model trained and saved successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error training model: {ex.Message}");
                throw;
            }
        }

        public async Task<List<NGORanking>> PredictNGODemandAsync(DonationMatchRequest donationRequest)
        {
            try
            {
                // Load model if not already loaded
                if (_model == null)
                {
                    if (File.Exists(_modelPath))
                    {
                        _model = _mlContext.Model.Load(_modelPath, out var modelSchema);
                    }
                    else
                    {
                        await TrainModelAsync();
                    }
                }

                // Get all NGOs
                var ngos = await _context.NGOs
                    .Include(n => n.User)
                    .Include(n => n.Donations)
                    .Include(n => n.DonationRequests)
                    .AsNoTracking()
                    .ToListAsync();

                var rankings = new List<NGORanking>();

                foreach (var ngo in ngos)
                {
                    // Calculate features for this NGO
                    var features = await CalculateNGOFeaturesAsync(ngo, donationRequest);
                    
                    // Create prediction input
                    var predictionInput = new NGOData
                    {
                        PastDonationsCount = features.PastDonationsCount,
                        PastDonationsTotalQuantity = features.PastDonationsTotalQuantity,
                        NGOCapacity = features.NGOCapacity,
                        LocationActivityScore = features.LocationActivityScore,
                        DistanceFromDonor = features.DistanceFromDonor,
                        TimeOfDay = features.TimeOfDay,
                        DayOfWeek = features.DayOfWeek,
                        Season = features.Season,
                        FoodTypeMatch = features.FoodTypeMatch,
                        ResponseTime = features.ResponseTime,
                        CompletionRate = features.CompletionRate
                    };

                    // Make prediction
                    var predictionEngine = _mlContext.Model.CreatePredictionEngine<NGOData, NGOPrediction>(_model!);
                    var prediction = predictionEngine.Predict(predictionInput);

                    // Calculate match score based on distance and other factors
                    var matchScore = CalculateMatchScore(ngo, donationRequest, features);

                    rankings.Add(new NGORanking
                    {
                        NGOId = ngo.Id,
                        NGOName = ngo.Name,
                        Contact = ngo.Contact,
                        Location = ngo.Location,
                        Capacity = ngo.Capacity,
                        DistanceKm = features.DistanceFromDonor,
                        PredictedDemandLevel = prediction.PredictedDemandLevel,
                        DemandScore = GetDemandScore(prediction),
                        MatchScore = matchScore,
                        Description = ngo.Description ?? "",
                        ResponseTime = features.ResponseTime,
                        CompletionRate = features.CompletionRate
                    });
                }

                // Sort by combined score (demand + match)
                return rankings.OrderByDescending(r => r.DemandScore + r.MatchScore).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error predicting NGO demand: {ex.Message}");
                return new List<NGORanking>();
            }
        }

        private async Task<List<NGOData>> GenerateTrainingDataAsync()
        {
            var trainingData = new List<NGOData>();

            // Get historical donations and requests
            var donations = await _context.Donations
                .Include(d => d.Donor)
                .Include(d => d.NGO)
                .Include(d => d.DonationRequests)
                .AsNoTracking()
                .ToListAsync();

            var ngos = await _context.NGOs
                .Include(n => n.Donations)
                .Include(n => n.DonationRequests)
                .AsNoTracking()
                .ToListAsync();

            foreach (var ngo in ngos)
            {
                var ngoDonations = donations.Where(d => d.NGOId == ngo.Id).ToList();
                var ngoRequests = ngo.DonationRequests.ToList();

                // Calculate features
                var pastDonationsCount = ngoDonations.Count;
                var pastDonationsTotalQuantity = ngoDonations.Sum(d => d.Quantity);
                var locationActivityScore = CalculateLocationActivityScore(ngo, donations);
                var responseTime = CalculateAverageResponseTime(ngoRequests);
                var completionRate = CalculateCompletionRate(ngoRequests);

                // Generate training samples for different scenarios
                foreach (var donation in ngoDonations)
                {
                    var features = new NGOData
                    {
                        PastDonationsCount = pastDonationsCount,
                        PastDonationsTotalQuantity = pastDonationsTotalQuantity,
                        NGOCapacity = ngo.Capacity,
                        LocationActivityScore = locationActivityScore,
                        DistanceFromDonor = CalculateDistance(ngo, donation.Donor),
                        TimeOfDay = donation.CreatedAt.Hour,
                        DayOfWeek = (float)donation.CreatedAt.DayOfWeek,
                        Season = GetSeason(donation.CreatedAt),
                        FoodTypeMatch = GetFoodTypeMatch(donation.FoodType, ngo),
                        ResponseTime = responseTime,
                        CompletionRate = completionRate,
                        DemandLevel = DetermineDemandLevel(ngo, ngoDonations, ngoRequests)
                    };

                    trainingData.Add(features);
                }
            }

            return trainingData;
        }

        private List<NGOData> GenerateSyntheticTrainingData()
        {
            var trainingData = new List<NGOData>();
            var random = new Random(42);

            for (int i = 0; i < 1000; i++)
            {
                var pastDonations = random.Next(0, 50);
                var totalQuantity = random.Next(0, 1000);
                var capacity = random.Next(100, 2000);
                var locationActivity = (float)random.NextDouble();
                var distance = (float)random.NextDouble() * 50;
                var timeOfDay = random.Next(0, 24);
                var dayOfWeek = random.Next(0, 7);
                var season = random.Next(1, 5);
                var foodTypeMatch = (float)random.NextDouble();
                var responseTime = (float)random.NextDouble() * 24;
                var completionRate = (float)random.NextDouble();

                var demandLevel = DetermineSyntheticDemandLevel(pastDonations, totalQuantity, capacity, 
                    locationActivity, responseTime, completionRate);

                trainingData.Add(new NGOData
                {
                    PastDonationsCount = pastDonations,
                    PastDonationsTotalQuantity = totalQuantity,
                    NGOCapacity = capacity,
                    LocationActivityScore = locationActivity,
                    DistanceFromDonor = distance,
                    TimeOfDay = timeOfDay,
                    DayOfWeek = dayOfWeek,
                    Season = season,
                    FoodTypeMatch = foodTypeMatch,
                    ResponseTime = responseTime,
                    CompletionRate = completionRate,
                    DemandLevel = demandLevel
                });
            }

            return trainingData;
        }

        private async Task<NGOFeatures> CalculateNGOFeaturesAsync(NGO ngo, DonationMatchRequest donationRequest)
        {
            var ngoDonations = ngo.Donations.ToList();
            var ngoRequests = ngo.DonationRequests.ToList();

            return new NGOFeatures
            {
                PastDonationsCount = ngoDonations.Count,
                PastDonationsTotalQuantity = ngoDonations.Sum(d => d.Quantity),
                NGOCapacity = ngo.Capacity,
                LocationActivityScore = CalculateLocationActivityScore(ngo, await _context.Donations.ToListAsync()),
                DistanceFromDonor = CalculateDistance(ngo, donationRequest.DonorLatitude, donationRequest.DonorLongitude),
                TimeOfDay = donationRequest.CreatedAt.Hour,
                DayOfWeek = (float)donationRequest.CreatedAt.DayOfWeek,
                Season = GetSeason(donationRequest.CreatedAt),
                FoodTypeMatch = GetFoodTypeMatch(donationRequest.FoodType, ngo),
                ResponseTime = CalculateAverageResponseTime(ngoRequests),
                CompletionRate = CalculateCompletionRate(ngoRequests)
            };
        }

        private float CalculateLocationActivityScore(NGO ngo, List<Donation> allDonations)
        {
            var nearbyDonations = allDonations.Where(d => 
                CalculateDistance(ngo, d.Donor?.Latitude ?? 0, d.Donor?.Longitude ?? 0) < 10).Count();
            
            return Math.Min(nearbyDonations / 10.0f, 1.0f);
        }

        private float CalculateDistance(NGO ngo, double latitude, double longitude)
        {
            if (!ngo.Latitude.HasValue || !ngo.Longitude.HasValue)
                return 50; // Default distance if no coordinates

            const double R = 6371; // Earth's radius in kilometers
            var dLat = ToRadians(latitude - ngo.Latitude.Value);
            var dLon = ToRadians(longitude - ngo.Longitude.Value);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(ngo.Latitude.Value)) * Math.Cos(ToRadians(latitude)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return (float)(R * c);
        }

        private float CalculateDistance(NGO ngo, ApplicationUser? donor)
        {
            if (donor?.Latitude == null || donor?.Longitude == null)
                return 50;

            return CalculateDistance(ngo, donor.Latitude.Value, donor.Longitude.Value);
        }

        private static double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        private float CalculateAverageResponseTime(List<DonationRequest> requests)
        {
            var completedRequests = requests.Where(r => r.RespondedAt.HasValue).ToList();
            if (!completedRequests.Any()) return 24; // Default 24 hours

            var totalHours = completedRequests.Sum(r => 
                (r.RespondedAt!.Value - r.CreatedAt).TotalHours);
            
            return (float)(totalHours / completedRequests.Count);
        }

        private float CalculateCompletionRate(List<DonationRequest> requests)
        {
            if (!requests.Any()) return 0.5f; // Default 50%

            var completedRequests = requests.Count(r => r.Status == "Completed");
            return (float)completedRequests / requests.Count;
        }

        private float GetSeason(DateTime date)
        {
            var month = date.Month;
            return month switch
            {
                >= 3 and <= 5 => 1, // Spring
                >= 6 and <= 8 => 2, // Summer
                >= 9 and <= 11 => 3, // Fall
                _ => 4 // Winter
            };
        }

        private float GetFoodTypeMatch(string foodType, NGO ngo)
        {
            // Simple food type matching logic
            var ngoDescription = ngo.Description?.ToLower() ?? "";
            var foodTypeLower = foodType.ToLower();

            if (ngoDescription.Contains(foodTypeLower) || 
                ngoDescription.Contains("food") || 
                ngoDescription.Contains("donation"))
                return 1.0f;

            return 0.5f; // Default match
        }

        private string DetermineDemandLevel(NGO ngo, List<Donation> ngoDonations, List<DonationRequest> ngoRequests)
        {
            var recentDonations = ngoDonations.Count(d => d.CreatedAt > DateTime.UtcNow.AddDays(-30));
            var recentRequests = ngoRequests.Count(r => r.CreatedAt > DateTime.UtcNow.AddDays(-30));
            var completionRate = CalculateCompletionRate(ngoRequests);

            if (recentDonations > 10 && recentRequests > 5 && completionRate > 0.8)
                return "High";
            else if (recentDonations > 5 && recentRequests > 2 && completionRate > 0.6)
                return "Medium";
            else
                return "Low";
        }

        private string DetermineSyntheticDemandLevel(int pastDonations, int totalQuantity, int capacity, 
            float locationActivity, float responseTime, float completionRate)
        {
            var score = (pastDonations * 0.3f) + (totalQuantity / 100.0f * 0.2f) + 
                       (locationActivity * 0.2f) + (completionRate * 0.2f) + 
                       (responseTime < 12 ? 0.1f : 0);

            if (score > 0.7) return "High";
            else if (score > 0.4) return "Medium";
            else return "Low";
        }

        private float GetDemandScore(NGOPrediction prediction)
        {
            return prediction.PredictedDemandLevel switch
            {
                "High" => 1.0f,
                "Medium" => 0.6f,
                "Low" => 0.3f,
                _ => 0.5f
            };
        }

        private float CalculateMatchScore(NGO ngo, DonationMatchRequest donationRequest, NGOFeatures features)
        {
            var distanceScore = Math.Max(0, 1 - (features.DistanceFromDonor / 50)); // Distance penalty
            var capacityScore = Math.Min(1, (float)ngo.Capacity / 1000); // Capacity bonus
            var foodTypeScore = features.FoodTypeMatch;
            var responseScore = Math.Max(0, 1 - (features.ResponseTime / 24)); // Response time bonus
            var completionScore = features.CompletionRate;

            return (distanceScore * 0.3f) + (capacityScore * 0.2f) + 
                   (foodTypeScore * 0.2f) + (responseScore * 0.15f) + (completionScore * 0.15f);
        }
    }

    public class NGOFeatures
    {
        public float PastDonationsCount { get; set; }
        public float PastDonationsTotalQuantity { get; set; }
        public float NGOCapacity { get; set; }
        public float LocationActivityScore { get; set; }
        public float DistanceFromDonor { get; set; }
        public float TimeOfDay { get; set; }
        public float DayOfWeek { get; set; }
        public float Season { get; set; }
        public float FoodTypeMatch { get; set; }
        public float ResponseTime { get; set; }
        public float CompletionRate { get; set; }
    }
}
