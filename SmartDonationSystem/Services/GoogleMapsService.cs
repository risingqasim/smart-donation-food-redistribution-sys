using Microsoft.Extensions.Configuration;
using SmartDonationSystem.Models;
using System.Text.Json;

namespace SmartDonationSystem.Services
{
    public class GoogleMapsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly int _maxDistanceKm;

        public GoogleMapsService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GoogleMaps:ApiKey"] ?? throw new InvalidOperationException("Google Maps API key not configured");
            _maxDistanceKm = configuration.GetValue<int>("GoogleMaps:MaxDistanceKm");
        }

        public async Task<Location?> GeocodeAddressAsync(string address)
        {
            try
            {
                var encodedAddress = Uri.EscapeDataString(address);
                var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={encodedAddress}&key={_apiKey}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GoogleGeocodeResponse>(json);

                if (result?.Status == "OK" && result.Results?.Any() == true)
                {
                    var location = result.Results.First().Geometry?.Location;
                    if (location != null)
                    {
                        return new Location
                        {
                            Latitude = location.Lat,
                            Longitude = location.Lng,
                            Address = result.Results.First().FormattedAddress
                        };
                    }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<LocationDistance?> CalculateDistanceAsync(Location from, Location to)
        {
            try
            {
                var origin = $"{from.Latitude},{from.Longitude}";
                var destination = $"{to.Latitude},{to.Longitude}";
                var url = $"https://maps.googleapis.com/maps/api/distancematrix/json?origins={origin}&destinations={destination}&units=metric&key={_apiKey}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GoogleDistanceMatrixResponse>(json);

                if (result?.Status == "OK" && result.Rows?.Any() == true)
                {
                    var element = result.Rows.First().Elements?.FirstOrDefault();
                    if (element?.Status == "OK")
                    {
                        return new LocationDistance
                        {
                            From = from,
                            To = to,
                            DistanceKm = element.Distance?.Value / 1000.0 ?? 0,
                            DistanceMiles = (element.Distance?.Value / 1000.0 ?? 0) * 0.621371,
                            DurationMinutes = element.Duration?.Value / 60 ?? 0,
                            DurationText = element.Duration?.Text
                        };
                    }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public double CalculateHaversineDistance(Location from, Location to)
        {
            const double R = 6371; // Earth's radius in kilometers
            var dLat = ToRadians(to.Latitude - from.Latitude);
            var dLon = ToRadians(to.Longitude - from.Longitude);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(from.Latitude)) * Math.Cos(ToRadians(to.Latitude)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }
    }

    // Google Maps API Response Models
    public class GoogleGeocodeResponse
    {
        public string Status { get; set; } = string.Empty;
        public List<GoogleGeocodeResult>? Results { get; set; }
    }

    public class GoogleGeocodeResult
    {
        public string FormattedAddress { get; set; } = string.Empty;
        public GoogleGeometry? Geometry { get; set; }
    }

    public class GoogleGeometry
    {
        public GoogleLocation? Location { get; set; }
    }

    public class GoogleLocation
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
    }

    public class GoogleDistanceMatrixResponse
    {
        public string Status { get; set; } = string.Empty;
        public List<GoogleDistanceMatrixRow>? Rows { get; set; }
    }

    public class GoogleDistanceMatrixRow
    {
        public List<GoogleDistanceMatrixElement>? Elements { get; set; }
    }

    public class GoogleDistanceMatrixElement
    {
        public string Status { get; set; } = string.Empty;
        public GoogleDistanceValue? Distance { get; set; }
        public GoogleDurationValue? Duration { get; set; }
    }

    public class GoogleDistanceValue
    {
        public int Value { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public class GoogleDurationValue
    {
        public int Value { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
