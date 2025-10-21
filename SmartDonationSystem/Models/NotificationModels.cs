namespace SmartDonationSystem.Models
{
    public class NotificationMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "info", "success", "warning", "error"
        public string Icon { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;
        public string? ActionUrl { get; set; }
        public string? RelatedEntityType { get; set; }
        public int? RelatedEntityId { get; set; }
        public string? SenderId { get; set; }
        public string? SenderName { get; set; }
    }

    public class DonationNotification
    {
        public int DonationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FoodType { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public string PickupAddress { get; set; } = string.Empty;
        public string DonorName { get; set; } = string.Empty;
        public string DonorContact { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DonationStatusNotification
    {
        public int DonationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // "Available", "Reserved", "Collected", "Expired"
        public string NGOName { get; set; } = string.Empty;
        public string NGOContact { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
        public string? Message { get; set; }
    }

    public class DonationRequestNotification
    {
        public int RequestId { get; set; }
        public int DonationId { get; set; }
        public string DonationTitle { get; set; } = string.Empty;
        public string NGOName { get; set; } = string.Empty;
        public string NGOContact { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // "Pending", "Approved", "Rejected", "Completed"
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class NotificationSettings
    {
        public string UserId { get; set; } = string.Empty;
        public bool EmailNotifications { get; set; } = true;
        public bool PushNotifications { get; set; } = true;
        public bool DonationNotifications { get; set; } = true;
        public bool RequestNotifications { get; set; } = true;
        public bool StatusNotifications { get; set; } = true;
        public bool SystemNotifications { get; set; } = true;
    }
}
