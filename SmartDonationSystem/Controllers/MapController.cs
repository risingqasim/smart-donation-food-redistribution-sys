using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDonationSystem.Data;
using SmartDonationSystem.Models;
using SmartDonationSystem.Services;
using System.Security.Claims;

namespace SmartDonationSystem.Controllers
{
    /// <summary>
    /// Controller for map-related operations
    /// Accessible to authenticated users (Donor, NGO, Admin)
    /// </summary>
    [Authorize(Roles = "Donor,NGO,Admin")]
    public class MapController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly GoogleMapsService _googleMapsService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MapController> _logger;

        public MapController(ApplicationDbContext context, GoogleMapsService googleMapsService, IConfiguration configuration, ILogger<MapController> logger)
        {
            _context = context;
            _googleMapsService = googleMapsService;
            _configuration = configuration;
            _logger = logger;
        }

        // GET: Map/Index
        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.GoogleMapsApiKey = _configuration["GoogleMaps:ApiKey"];
            return View();
        }

        // GET: Map/DonationMap
        [HttpGet]
        public async Task<IActionResult> DonationMap()
        {
            try
            {
                var donations = await _context.Donations
                    .Include(d => d.Donor)
                    .Include(d => d.NGO)
                    .Where(d => d.Status == "Available")
                    .AsNoTracking()
                    .ToListAsync();

                var markers = donations.Select(d => new MapMarker
                {
                    Latitude = d.Donor?.Latitude ?? 0,
                    Longitude = d.Donor?.Longitude ?? 0,
                    Title = d.Title,
                    Description = $"{d.Description} - {d.FoodType} ({d.Quantity} {d.Unit})",
                    Type = "donation",
                    EntityId = d.Id,
                    IconUrl = "/images/donation-marker.png"
                }).Where(m => m.Latitude != 0 && m.Longitude != 0).ToList();

                return Json(markers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading donation map data");
                return BadRequest(new { error = "Failed to load donation data" });
            }
        }

        // GET: Map/NGOMap
        public async Task<IActionResult> NGOMap()
        {
            var ngos = await _context.NGOs
                .Include(n => n.User)
                .AsNoTracking()
                .ToListAsync();

            var markers = ngos.Select(n => new MapMarker
            {
                Latitude = n.Latitude ?? 0,
                Longitude = n.Longitude ?? 0,
                Title = n.Name,
                Description = $"{n.Description} - Capacity: {n.Capacity}",
                Type = "ngo",
                EntityId = n.Id,
                IconUrl = "/images/ngo-marker.png"
            }).Where(m => m.Latitude != 0 && m.Longitude != 0).ToList();

            return View(markers);
        }

        // POST: Map/GeocodeAddress
        [HttpPost]
        public async Task<IActionResult> GeocodeAddress([FromBody] GeocodeRequest request)
        {
            if (string.IsNullOrEmpty(request.Address))
            {
                return BadRequest("Address is required");
            }

            var location = await _googleMapsService.GeocodeAddressAsync(request.Address);
            if (location == null)
            {
                return NotFound("Address not found");
            }

            return Ok(location);
        }

        // POST: Map/CalculateDistance
        [HttpPost]
        public async Task<IActionResult> CalculateDistance([FromBody] DistanceRequest request)
        {
            if (request.From == null || request.To == null)
            {
                return BadRequest("From and To locations are required");
            }

            var distance = await _googleMapsService.CalculateDistanceAsync(request.From, request.To);
            if (distance == null)
            {
                return BadRequest("Unable to calculate distance");
            }

            return Ok(distance);
        }

        // GET: Map/NearbyNGOs
        [HttpGet]
        public async Task<IActionResult> NearbyNGOs(double latitude, double longitude, double radiusKm = 50)
        {
            try
            {
                // Validate input parameters
                if (latitude < -90 || latitude > 90)
                {
                    return BadRequest(new { error = "Invalid latitude. Must be between -90 and 90." });
                }
                if (longitude < -180 || longitude > 180)
                {
                    return BadRequest(new { error = "Invalid longitude. Must be between -180 and 180." });
                }
                if (radiusKm < 0 || radiusKm > 1000)
                {
                    return BadRequest(new { error = "Invalid radius. Must be between 0 and 1000 km." });
                }

                var userLocation = new Location { Latitude = latitude, Longitude = longitude };
                
                var ngos = await _context.NGOs
                    .Include(n => n.User)
                    .Where(n => n.Latitude.HasValue && n.Longitude.HasValue)
                    .AsNoTracking()
                    .ToListAsync();

                var nearbyNGOs = new List<NearbyNGO>();

                foreach (var ngo in ngos)
                {
                    var ngoLocation = new Location 
                    { 
                        Latitude = ngo.Latitude!.Value, 
                        Longitude = ngo.Longitude!.Value 
                    };

                    var distance = _googleMapsService.CalculateHaversineDistance(userLocation, ngoLocation);
                    
                    if (distance <= radiusKm)
                    {
                        nearbyNGOs.Add(new NearbyNGO
                        {
                            NGOId = ngo.Id,
                            Name = ngo.Name,
                            Contact = ngo.Contact,
                            Location = ngoLocation,
                            DistanceKm = Math.Round(distance, 2),
                            DurationMinutes = (int)(distance * 1.5), // Approximate driving time
                            Description = ngo.Description,
                            Capacity = ngo.Capacity
                        });
                    }
                }

                return Ok(nearbyNGOs.OrderBy(n => n.DistanceKm));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading nearby NGOs");
                return BadRequest(new { error = "Failed to load nearby NGOs" });
            }
        }
    }

    public class GeocodeRequest
    {
        public string Address { get; set; } = string.Empty;
    }

    public class DistanceRequest
    {
        public Location? From { get; set; }
        public Location? To { get; set; }
    }
}
