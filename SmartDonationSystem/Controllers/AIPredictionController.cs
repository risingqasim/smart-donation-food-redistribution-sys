using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDonationSystem.ML;
using SmartDonationSystem.Services;

namespace SmartDonationSystem.Controllers
{
    /// <summary>
    /// Controller for AI prediction endpoints
    /// Accessible to NGO and Admin roles
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "NGO,Admin")]
    public class AIPredictionController : ControllerBase
    {
        private readonly AIPredictionService _predictionService;
        private readonly ILogger<AIPredictionController> _logger;

        public AIPredictionController(
            AIPredictionService predictionService,
            ILogger<AIPredictionController> logger)
        {
            _predictionService = predictionService;
            _logger = logger;
        }

        /// <summary>
        /// POST: api/AIPrediction/PredictFoodDemand
        /// Predicts food demand level (High, Medium, Low) based on historical data
        /// </summary>
        [HttpPost("PredictFoodDemand")]
        public async Task<ActionResult<FoodDemandPredictionResult>> PredictFoodDemand([FromBody] FoodDemandPredictionRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Request body is required.");
                }

                var result = await _predictionService.PredictFoodDemandAsync(request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error predicting food demand");
                return StatusCode(500, new { error = "An error occurred while predicting food demand.", message = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/AIPrediction/PredictFoodDemand
        /// Predicts food demand level with query parameters
        /// </summary>
        [HttpGet("PredictFoodDemand")]
        public async Task<ActionResult<FoodDemandPredictionResult>> PredictFoodDemandGet(
            [FromQuery] DateTime? date = null,
            [FromQuery] string? foodType = null,
            [FromQuery] string? location = null)
        {
            try
            {
                var request = new FoodDemandPredictionRequest
                {
                    Date = date,
                    FoodType = foodType,
                    Location = location
                };

                var result = await _predictionService.PredictFoodDemandAsync(request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error predicting food demand");
                return StatusCode(500, new { error = "An error occurred while predicting food demand.", message = ex.Message });
            }
        }

        /// <summary>
        /// POST: api/AIPrediction/TrainModel
        /// Trains the ML model using historical donation data
        /// </summary>
        [HttpPost("TrainModel")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> TrainModel()
        {
            try
            {
                await _predictionService.TrainModelAsync();
                return Ok(new { message = "Model trained successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training model");
                return StatusCode(500, new { error = "An error occurred while training the model.", message = ex.Message });
            }
        }
    }
}

