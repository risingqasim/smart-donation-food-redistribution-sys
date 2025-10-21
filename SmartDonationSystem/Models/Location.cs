using System.ComponentModel.DataAnnotations;

namespace SmartDonationSystem.Models
{
    public class Location
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }
    }

    public class LocationDistance
    {
        public Location From { get; set; } = new Location();
        public Location To { get; set; } = new Location();
        public double DistanceKm { get; set; }
        public double DistanceMiles { get; set; }
        public int DurationMinutes { get; set; }
        public string? DurationText { get; set; }
    }

    public class NearbyNGO
    {
        public int NGOId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public Location Location { get; set; } = new Location();
        public double DistanceKm { get; set; }
        public int DurationMinutes { get; set; }
        public string? Description { get; set; }
        public int Capacity { get; set; }
    }

    public class MapMarker
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "donor", "ngo", "donation"
        public int? EntityId { get; set; }
        public string? IconUrl { get; set; }
    }
}
