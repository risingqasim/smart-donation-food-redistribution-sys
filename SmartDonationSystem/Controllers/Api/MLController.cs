using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDonationSystem.Data;
using SmartDonationSystem.Models.ML;
using SmartDonationSystem.Services;
using System.Security.Claims;

namespace SmartDonationSystem.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MLController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly MLService _mlService;

        public MLController(ApplicationDbContext context, MLService mlService)
        {
            _context = context;
            _mlService = mlService;
        }

        // POST: api/ML/PredictNGODemand
        [HttpPost("PredictNGODemand")]
        public async Task<ActionResult<List<NGORanking>>> PredictNGODemand([FromBody] DonationMatchRequest request)
        {
            try
            {
                var rankings = await _mlService.PredictNGODemandAsync(request);
                return Ok(rankings);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error predicting NGO demand: {ex.Message}");
            }
        }

        // POST: api/ML/GetRecommendedNGOs
        [HttpPost("GetRecommendedNGOs")]
        public async Task<ActionResult<List<NGORanking>>> GetRecommendedNGOs([FromBody] DonationMatchRequest request)
        {
            try
            {
                var rankings = await _mlService.PredictNGODemandAsync(request);
                
                // Filter and return top 10 recommendations
                var recommendations = rankings
                    .Where(r => r.DistanceKm <= 50) // Within 50km
                    .Take(10)
                    .ToList();

                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error getting recommended NGOs: {ex.Message}");
            }
        }

        // GET: api/ML/GetNGORecommendations/{donationId}
        [HttpGet("GetNGORecommendations/{donationId}")]
        public async Task<ActionResult<List<NGORanking>>> GetNGORecommendations(int donationId)
        {
            try
            {
                var donation = await _context.Donations
                    .Include(d => d.Donor)
                    .FirstOrDefaultAsync(d => d.Id == donationId);

                if (donation == null)
                {
                    return NotFound("Donation not found");
                }

                var request = new DonationMatchRequest
                {
                    DonationId = donation.Id,
                    FoodType = donation.FoodType,
                    Quantity = donation.Quantity,
                    Unit = donation.Unit ?? "",
                    ExpiryDate = donation.ExpiryDate,
                    DonorLatitude = donation.Donor?.Latitude ?? 0,
                    DonorLongitude = donation.Donor?.Longitude ?? 0,
                    DonorLocation = donation.PickupAddress,
                    CreatedAt = donation.CreatedAt
                };

                var rankings = await _mlService.PredictNGODemandAsync(request);
                
                // Return top 5 recommendations
                var recommendations = rankings.Take(5).ToList();
                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error getting NGO recommendations: {ex.Message}");
            }
        }

        // POST: api/ML/TrainModel
        [HttpPost("TrainModel")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> TrainModel()
        {
            try
            {
                await _mlService.TrainModelAsync();
                return Ok("Model trained successfully");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error training model: {ex.Message}");
            }
        }

        // GET: api/ML/GetNGODemandLevel/{ngoId}
        [HttpGet("GetNGODemandLevel/{ngoId}")]
        public async Task<ActionResult<object>> GetNGODemandLevel(int ngoId)
        {
            try
            {
                var ngo = await _context.NGOs
                    .Include(n => n.User)
                    .Include(n => n.Donations)
                    .Include(n => n.DonationRequests)
                    .FirstOrDefaultAsync(n => n.Id == ngoId);

                if (ngo == null)
                {
                    return NotFound("NGO not found");
                }

                // Create a sample request for prediction
                var sampleRequest = new DonationMatchRequest
                {
                    FoodType = "Vegetables",
                    Quantity = 50,
                    Unit = "kg",
                    ExpiryDate = DateTime.UtcNow.AddDays(3),
                    DonorLatitude = 40.7128,
                    DonorLongitude = -74.0060,
                    DonorLocation = "Sample Location",
                    CreatedAt = DateTime.UtcNow
                };

                var rankings = await _mlService.PredictNGODemandAsync(sampleRequest);
                var ngoRanking = rankings.FirstOrDefault(r => r.NGOId == ngoId);

                if (ngoRanking == null)
                {
                    return NotFound("NGO not found in predictions");
                }

                return Ok(new
                {
                    NGOId = ngo.Id,
                    NGOName = ngo.Name,
                    PredictedDemandLevel = ngoRanking.PredictedDemandLevel,
                    DemandScore = ngoRanking.DemandScore,
                    MatchScore = ngoRanking.MatchScore,
                    DistanceKm = ngoRanking.DistanceKm,
                    Capacity = ngoRanking.Capacity,
                    ResponseTime = ngoRanking.ResponseTime,
                    CompletionRate = ngoRanking.CompletionRate
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error getting NGO demand level: {ex.Message}");
            }
        }

        // GET: api/ML/GetAllNGODemandLevels
        [HttpGet("GetAllNGODemandLevels")]
        public async Task<ActionResult<List<object>>> GetAllNGODemandLevels()
        {
            try
            {
                var ngos = await _context.NGOs
                    .Include(n => n.User)
                    .Include(n => n.Donations)
                    .Include(n => n.DonationRequests)
                    .AsNoTracking()
                    .ToListAsync();

                var results = new List<object>();

                foreach (var ngo in ngos)
                {
                    var sampleRequest = new DonationMatchRequest
                    {
                        FoodType = "Mixed",
                        Quantity = 100,
                        Unit = "kg",
                        ExpiryDate = DateTime.UtcNow.AddDays(2),
                        DonorLatitude = 40.7128,
                        DonorLongitude = -74.0060,
                        DonorLocation = "Sample Location",
                        CreatedAt = DateTime.UtcNow
                    };

                    var rankings = await _mlService.PredictNGODemandAsync(sampleRequest);
                    var ngoRanking = rankings.FirstOrDefault(r => r.NGOId == ngo.Id);

                    if (ngoRanking != null)
                    {
                        results.Add(new
                        {
                            NGOId = ngo.Id,
                            NGOName = ngo.Name,
                            PredictedDemandLevel = ngoRanking.PredictedDemandLevel,
                            DemandScore = ngoRanking.DemandScore,
                            MatchScore = ngoRanking.MatchScore,
                            DistanceKm = ngoRanking.DistanceKm,
                            Capacity = ngoRanking.Capacity,
                            ResponseTime = ngoRanking.ResponseTime,
                            CompletionRate = ngoRanking.CompletionRate
                        });
                    }
                }

                return Ok(results.OrderByDescending(r => ((dynamic)r).DemandScore).ToList());
            }
            catch (Exception ex)
            {
                return BadRequest($"Error getting all NGO demand levels: {ex.Message}");
            }
        }
    }
}
