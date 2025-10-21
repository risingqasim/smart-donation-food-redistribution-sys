using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartDonationSystem.Models
{
    public class DonationRequest
    {
        public int Id { get; set; }

        [Required]
        public int DonationId { get; set; }
        public Donation? Donation { get; set; }

        [Required]
        public int NGOId { get; set; }
        public NGO? NGO { get; set; }

        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Completed

        [StringLength(200)]
        public string? ResponseMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
    }
}
