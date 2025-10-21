using System.ComponentModel.DataAnnotations;

namespace SmartDonationSystem.Models
{
    public class NGO
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Location { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Contact { get; set; } = string.Empty;

        [Required]
        public int Capacity { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(200)]
        public string? Website { get; set; }

        [StringLength(50)]
        public string? RegistrationNumber { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public ICollection<Donation> Donations { get; set; } = new List<Donation>();
        public ICollection<DonationRequest> DonationRequests { get; set; } = new List<DonationRequest>();
    }
}
