# Smart Donation System - Google Maps Integration Guide

## Overview

The Smart Donation System now includes comprehensive Google Maps integration to display donor and NGO locations, calculate distances, and suggest nearby NGOs for donations.

## Features Implemented

### 🗺️ **Interactive Map Views**
- **Main Map View:** Interactive map showing all donations and NGOs
- **Donation Creation Map:** Integrated map in donation creation form
- **Location Search:** Address geocoding and reverse geocoding
- **Distance Calculation:** Real-time distance and travel time calculations

### 📍 **Location Services**
- **Geocoding:** Convert addresses to coordinates
- **Reverse Geocoding:** Convert coordinates to addresses
- **Distance Matrix:** Calculate driving distances and times
- **Nearby Search:** Find NGOs within specified radius

### 🎯 **Role-Based Map Features**

#### **Donor Features:**
- Interactive map in donation creation form
- Click to set pickup location
- Address geocoding for pickup addresses
- Visual confirmation of donation location

#### **NGO Features:**
- View all available donations on map
- Find nearby donations within radius
- Distance calculations to donation locations
- Organization location management

#### **Admin Features:**
- System-wide map overview
- All donations and NGOs displayed
- Distance analytics and reporting
- Location management tools

## Technical Implementation

### **Models Created:**

#### **Location.cs**
```csharp
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
```

#### **LocationDistance.cs**
```csharp
public class LocationDistance
{
    public Location From { get; set; }
    public Location To { get; set; }
    public double DistanceKm { get; set; }
    public double DistanceMiles { get; set; }
    public int DurationMinutes { get; set; }
    public string? DurationText { get; set; }
}
```

#### **NearbyNGO.cs**
```csharp
public class NearbyNGO
{
    public int NGOId { get; set; }
    public string Name { get; set; }
    public string Contact { get; set; }
    public Location Location { get; set; }
    public double DistanceKm { get; set; }
    public int DurationMinutes { get; set; }
    public string? Description { get; set; }
    public int Capacity { get; set; }
}
```

### **Services Implemented:**

#### **GoogleMapsService.cs**
- **GeocodeAddressAsync:** Convert address to coordinates
- **CalculateDistanceAsync:** Get driving distance and time
- **CalculateHaversineDistance:** Calculate straight-line distance
- **HTTP client integration** for Google Maps API calls

### **Controllers Created:**

#### **MapController.cs**
- **Index:** Main interactive map view
- **DonationMap:** Map showing all donations
- **NGOMap:** Map showing all NGOs
- **GeocodeAddress:** Address geocoding endpoint
- **CalculateDistance:** Distance calculation endpoint
- **NearbyNGOs:** Find nearby NGOs within radius

### **Database Updates:**

#### **New Fields Added:**
- **ApplicationUser:** `Latitude`, `Longitude`
- **NGO:** `Latitude`, `Longitude`
- **Migration:** `AddLocationCoordinates` applied

## API Endpoints

### **Map Endpoints:**

#### **GET /Map/Index**
Interactive map with search and filtering capabilities.

#### **GET /Map/DonationMap**
Map view showing all available donations with markers.

#### **GET /Map/NGOMap**
Map view showing all NGO organizations with markers.

#### **POST /Map/GeocodeAddress**
```json
{
  "address": "123 Main St, New York, NY"
}
```
**Response:**
```json
{
  "latitude": 40.7128,
  "longitude": -74.0060,
  "address": "123 Main St, New York, NY 10001, USA"
}
```

#### **POST /Map/CalculateDistance**
```json
{
  "from": {
    "latitude": 40.7128,
    "longitude": -74.0060
  },
  "to": {
    "latitude": 40.7589,
    "longitude": -73.9851
  }
}
```
**Response:**
```json
{
  "from": { "latitude": 40.7128, "longitude": -74.0060 },
  "to": { "latitude": 40.7589, "longitude": -73.9851 },
  "distanceKm": 5.2,
  "distanceMiles": 3.2,
  "durationMinutes": 15,
  "durationText": "15 mins"
}
```

#### **GET /Map/NearbyNGOs**
```
GET /Map/NearbyNGOs?latitude=40.7128&longitude=-74.0060&radiusKm=50
```
**Response:**
```json
[
  {
    "ngoId": 1,
    "name": "Food Bank Central",
    "contact": "contact@foodbank.org",
    "location": {
      "latitude": 40.7589,
      "longitude": -73.9851
    },
    "distanceKm": 5.2,
    "durationMinutes": 15,
    "description": "Community food bank",
    "capacity": 1000
  }
]
```

## Configuration

### **appsettings.json**
```json
{
  "GoogleMaps": {
    "ApiKey": "YOUR_GOOGLE_MAPS_API_KEY_HERE",
    "DefaultZoom": 12,
    "MaxDistanceKm": 50
  }
}
```

### **Required Google Maps APIs:**
1. **Maps JavaScript API** - For interactive maps
2. **Geocoding API** - For address conversion
3. **Distance Matrix API** - For distance calculations
4. **Places API** - For location search (optional)

## Usage Examples

### **Donation Creation with Map:**

1. **User navigates to Create Donation**
2. **Map loads with user's current location**
3. **User can:**
   - Enter address in text field and click "Get Location on Map"
   - Click directly on map to set pickup location
   - Use geolocation to center map on current position
4. **Coordinates are automatically saved** with donation

### **Finding Nearby NGOs:**

1. **User navigates to Map page**
2. **System requests location permission**
3. **User clicks "Find Nearby NGOs"**
4. **System displays:**
   - Map markers for nearby NGOs
   - List of NGOs with distances
   - Clickable items to center map on specific NGO

### **Distance Calculations:**

1. **NGO requests donation**
2. **System calculates distance** from NGO to donor
3. **Distance displayed** in request details
4. **Travel time estimated** for pickup planning

## JavaScript Integration

### **Map Initialization:**
```javascript
function initMap() {
    map = new google.maps.Map(document.getElementById("map"), {
        center: { lat: 40.7128, lng: -74.0060 },
        zoom: 12
    });
}
```

### **Address Geocoding:**
```javascript
function geocodeAddress(address) {
    fetch('/Map/GeocodeAddress', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ address: address })
    })
    .then(response => response.json())
    .then(data => {
        // Handle geocoded location
    });
}
```

### **Distance Calculation:**
```javascript
function calculateDistance(from, to) {
    fetch('/Map/CalculateDistance', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ from: from, to: to })
    })
    .then(response => response.json())
    .then(data => {
        // Handle distance result
    });
}
```

## Security Considerations

### **API Key Protection:**
- Google Maps API key stored in configuration
- Server-side geocoding to protect API key
- Rate limiting on API endpoints
- Input validation for all address data

### **User Privacy:**
- Location data only collected with user consent
- Coordinates stored securely in database
- User can opt out of location services
- Data anonymization for analytics

## Performance Optimization

### **Caching:**
- Geocoded addresses cached to reduce API calls
- Distance calculations cached for repeated requests
- Map tiles cached by Google Maps

### **Lazy Loading:**
- Map loads only when needed
- Markers loaded dynamically
- Large datasets paginated

### **Efficient Queries:**
- Spatial queries for nearby searches
- Indexed latitude/longitude fields
- Optimized distance calculations

## Future Enhancements

### **Planned Features:**
- **Route Optimization:** Multi-stop pickup routes
- **Real-time Tracking:** Live donation pickup tracking
- **Heat Maps:** Donation density visualization
- **Clustering:** Group nearby markers
- **Offline Maps:** Cached map data
- **Mobile Integration:** Native app maps

### **Advanced Analytics:**
- **Donation Patterns:** Geographic analysis
- **NGO Coverage:** Service area mapping
- **Efficiency Metrics:** Distance-based optimization
- **Demand Forecasting:** Location-based predictions

## Troubleshooting

### **Common Issues:**

#### **Map Not Loading:**
- Check Google Maps API key configuration
- Verify API key has required permissions
- Check browser console for JavaScript errors

#### **Geocoding Fails:**
- Verify address format
- Check API quota limits
- Ensure Geocoding API is enabled

#### **Distance Calculation Errors:**
- Verify Distance Matrix API is enabled
- Check API quota and billing
- Validate coordinate data

### **Debug Mode:**
```javascript
// Enable debug logging
console.log('Map initialized:', map);
console.log('Geocoding result:', data);
console.log('Distance calculation:', distance);
```

## Testing

### **Test Scenarios:**
1. **Create donation with map location**
2. **Search for address and verify geocoding**
3. **Find nearby NGOs within radius**
4. **Calculate distance between locations**
5. **Verify map markers display correctly**

### **Test Data:**
- **Sample addresses** for geocoding tests
- **Known coordinates** for distance calculations
- **Test NGO locations** for nearby searches

## Documentation References

- [Google Maps JavaScript API](https://developers.google.com/maps/documentation/javascript)
- [Google Geocoding API](https://developers.google.com/maps/documentation/geocoding)
- [Google Distance Matrix API](https://developers.google.com/maps/documentation/distance-matrix)
- [ASP.NET Core Configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration)

The Google Maps integration provides a comprehensive location-based experience for the Smart Donation System, enabling users to visualize, navigate, and optimize donation distribution across geographic areas.
