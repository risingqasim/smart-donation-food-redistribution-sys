using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartDonationSystem.Models
{
    /// <summary>
    /// Represents ML predictions made for donation matching and NGO demand forecasting
    /// </summary>
    public class Prediction
    {
        public int Id { get; set; }

        /// <summary>
        /// The donation for which this prediction was made
        /// </summary>
        public int? DonationId { get; set; }
        public Donation? Donation { get; set; }

        /// <summary>
        /// The NGO for which this prediction was made
        /// </summary>
        public int? NGOId { get; set; }
        public NGO? NGO { get; set; }

        /// <summary>
        /// Type of prediction: DemandLevel, MatchScore, SuccessProbability, etc.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string PredictionType { get; set; } = string.Empty;

        /// <summary>
        /// The predicted value (e.g., "High", "Medium", "Low" for demand level)
        /// </summary>
        [Required]
        [StringLength(100)]
        public string PredictedValue { get; set; } = string.Empty;

        /// <summary>
        /// Confidence score (0.0 to 1.0)
        /// </summary>
        [Column(TypeName = "decimal(5,4)")]
        public decimal ConfidenceScore { get; set; }

        /// <summary>
        /// Match score calculated for donation-NGO matching
        /// </summary>
        [Column(TypeName = "decimal(5,4)")]
        public decimal? MatchScore { get; set; }

        /// <summary>
        /// Demand score calculated for NGO demand prediction
        /// </summary>
        [Column(TypeName = "decimal(5,4)")]
        public decimal? DemandScore { get; set; }

        /// <summary>
        /// Distance in kilometers between donor and NGO
        /// </summary>
        [Column(TypeName = "decimal(10,2)")]
        public decimal? DistanceKm { get; set; }

        /// <summary>
        /// Additional prediction metadata stored as JSON
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? Metadata { get; set; }

        /// <summary>
        /// Whether the prediction was accurate (set after actual outcome)
        /// </summary>
        public bool? IsAccurate { get; set; }

        /// <summary>
        /// Actual outcome for comparison (set after prediction outcome is known)
        /// </summary>
        [StringLength(100)]
        public string? ActualOutcome { get; set; }

        /// <summary>
        /// Timestamp when prediction was made
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when actual outcome was recorded
        /// </summary>
        public DateTime? OutcomeRecordedAt { get; set; }

        /// <summary>
        /// User who triggered this prediction (if applicable)
        /// </summary>
        public string? CreatedByUserId { get; set; }
        public ApplicationUser? CreatedByUser { get; set; }
    }
}

