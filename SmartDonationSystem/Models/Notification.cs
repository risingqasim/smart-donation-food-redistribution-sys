using System.ComponentModel.DataAnnotations;

namespace SmartDonationSystem.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Title { get; set; }

        [StringLength(50)]
        public string? Type { get; set; } // Info, Warning, Success, Error

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        public DateTime? ReadAt { get; set; }

        [StringLength(200)]
        public string? ActionUrl { get; set; }

        public int? RelatedEntityId { get; set; } // For linking to donations, requests, etc.
        public string? RelatedEntityType { get; set; } // Donation, DonationRequest, etc.
    }
}
