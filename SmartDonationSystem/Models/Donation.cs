using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartDonationSystem.Models
{
    public class Donation
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string FoodType { get; set; } = string.Empty; // Vegetables, Fruits, Grains, Dairy, etc.

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Available"; // Available, Reserved, Collected, Expired

        [Required]
        public int Quantity { get; set; }

        [StringLength(50)]
        public string? Unit { get; set; } // pieces, kg, liters, etc.

        public DateTime ExpiryDate { get; set; }

        [Required]
        [StringLength(500)]
        public string PickupAddress { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        [Required]
        public string DonorId { get; set; } = string.Empty;
        public ApplicationUser? Donor { get; set; }

        public int? NGOId { get; set; }
        public NGO? NGO { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CollectedAt { get; set; }

        // Navigation properties
        public ICollection<DonationRequest> DonationRequests { get; set; } = new List<DonationRequest>();
        public ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();
    }
}
