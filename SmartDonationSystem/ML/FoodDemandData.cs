using Microsoft.ML.Data;

namespace SmartDonationSystem.ML
{
    /// <summary>
    /// Input data model for food demand prediction
    /// </summary>
    public class FoodDemandData
    {
        [LoadColumn(0)]
        public float Month { get; set; }

        [LoadColumn(1)]
        public float DayOfWeek { get; set; }

        [LoadColumn(2)]
        public float Season { get; set; }

        [LoadColumn(3)]
        public float FoodTypeIndex { get; set; }

        [LoadColumn(4)]
        public float AverageQuantity { get; set; }

        [LoadColumn(5)]
        public float DonationFrequency { get; set; }

        [LoadColumn(6)]
        public float RequestFrequency { get; set; }

        [LoadColumn(7)]
        public float CompletionRate { get; set; }

        [LoadColumn(8)]
        public float LocationActivity { get; set; }

        [LoadColumn(9)]
        public string DemandLevel { get; set; } = string.Empty; // High, Medium, Low
    }

    /// <summary>
    /// Prediction output model for food demand
    /// </summary>
    public class FoodDemandPrediction
    {
        [ColumnName("PredictedLabel")]
        public string PredictedDemandLevel { get; set; } = string.Empty;

        [ColumnName("Score")]
        public float[] Score { get; set; } = new float[0];

        public float HighDemandScore => Score.Length > 0 ? Score[0] : 0;
        public float MediumDemandScore => Score.Length > 1 ? Score[1] : 0;
        public float LowDemandScore => Score.Length > 2 ? Score[2] : 0;

        public float Confidence => Score.Length > 0 ? Score.Max() : 0;
    }

    /// <summary>
    /// Request model for food demand prediction
    /// </summary>
    public class FoodDemandPredictionRequest
    {
        public DateTime? Date { get; set; }
        public string? FoodType { get; set; }
        public string? Location { get; set; }
        public int? RegionId { get; set; }
    }

    /// <summary>
    /// Response model for food demand prediction
    /// </summary>
    public class FoodDemandPredictionResult
    {
        public string PredictedDemandLevel { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public float HighDemandProbability { get; set; }
        public float MediumDemandProbability { get; set; }
        public float LowDemandProbability { get; set; }
        public Dictionary<string, float> FeatureImportance { get; set; } = new Dictionary<string, float>();
        public DateTime PredictedDate { get; set; }
        public string? FoodType { get; set; }
    }
}

