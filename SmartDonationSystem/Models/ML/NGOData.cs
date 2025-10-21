using Microsoft.ML.Data;

namespace SmartDonationSystem.Models.ML
{
    public class NGOData
    {
        [LoadColumn(0)]
        public float PastDonationsCount { get; set; }

        [LoadColumn(1)]
        public float PastDonationsTotalQuantity { get; set; }

        [LoadColumn(2)]
        public float NGOCapacity { get; set; }

        [LoadColumn(3)]
        public float LocationActivityScore { get; set; }

        [LoadColumn(4)]
        public float DistanceFromDonor { get; set; }

        [LoadColumn(5)]
        public float TimeOfDay { get; set; }

        [LoadColumn(6)]
        public float DayOfWeek { get; set; }

        [LoadColumn(7)]
        public float Season { get; set; }

        [LoadColumn(8)]
        public float FoodTypeMatch { get; set; }

        [LoadColumn(9)]
        public float ResponseTime { get; set; }

        [LoadColumn(10)]
        public float CompletionRate { get; set; }

        [LoadColumn(11)]
        public string DemandLevel { get; set; } = string.Empty;
    }

    public class NGOPrediction
    {
        [ColumnName("PredictedLabel")]
        public string PredictedDemandLevel { get; set; } = string.Empty;

        [ColumnName("Score")]
        public float[] Score { get; set; } = new float[0];

        public float HighDemandScore => Score.Length > 0 ? Score[0] : 0;
        public float MediumDemandScore => Score.Length > 1 ? Score[1] : 0;
        public float LowDemandScore => Score.Length > 2 ? Score[2] : 0;
    }

    public class NGORanking
    {
        public int NGOId { get; set; }
        public string NGOName { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public double DistanceKm { get; set; }
        public string PredictedDemandLevel { get; set; } = string.Empty;
        public float DemandScore { get; set; }
        public float MatchScore { get; set; }
        public string Description { get; set; } = string.Empty;
        public float ResponseTime { get; set; }
        public float CompletionRate { get; set; }
    }

    public class DonationMatchRequest
    {
        public int DonationId { get; set; }
        public string FoodType { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public double DonorLatitude { get; set; }
        public double DonorLongitude { get; set; }
        public string DonorLocation { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
