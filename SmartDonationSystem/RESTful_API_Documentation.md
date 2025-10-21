# Smart Donation System - RESTful API Documentation

## Overview

The Smart Donation System provides comprehensive RESTful APIs for donation management with role-based access control. All API endpoints require JWT authentication.

## Base URL
```
https://localhost:5001/api
```

## Authentication

All API endpoints require a valid JWT token in the Authorization header:
```
Authorization: Bearer {your_jwt_token}
```

## API Endpoints

### Authentication Endpoints

#### POST /api/auth/register
Register a new user with role assignment.

**Request Body:**
```json
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john@example.com",
  "password": "Password123!",
  "confirmPassword": "Password123!",
  "role": "Donor",
  "address": "123 Main St",
  "city": "New York",
  "postalCode": "10001",
  "state": "NY",
  "country": "USA"
}
```

**Response:**
```json
{
  "token": "jwt_token_here",
  "userId": "user_id",
  "email": "john@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "roles": ["Donor"],
  "expiresAt": "2025-01-21T17:00:00Z"
}
```

#### POST /api/auth/login
Login with email and password.

**Request Body:**
```json
{
  "email": "john@example.com",
  "password": "Password123!"
}
```

**Response:**
```json
{
  "token": "jwt_token_here",
  "userId": "user_id",
  "email": "john@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "roles": ["Donor"],
  "expiresAt": "2025-01-21T17:00:00Z"
}
```

### Donation Management Endpoints

#### GET /api/donations
Get all donations with optional filtering.

**Query Parameters:**
- `category` (optional): Filter by food type
- `status` (optional): Filter by status (Available, Reserved, Collected, Expired)

**Headers:**
- `Authorization: Bearer {jwt_token}`

**Response:**
```json
[
  {
    "id": 1,
    "title": "Fresh Vegetables",
    "description": "Organic vegetables from local farm",
    "foodType": "Vegetables",
    "status": "Available",
    "quantity": 50,
    "unit": "kg",
    "expiryDate": "2025-01-25T00:00:00Z",
    "pickupAddress": "123 Farm Road, Green Valley",
    "imageUrl": "https://example.com/image.jpg",
    "location": "Green Valley",
    "donorId": "donor_user_id",
    "ngoId": null,
    "createdAt": "2025-01-21T10:00:00Z",
    "updatedAt": null,
    "collectedAt": null,
    "donor": {
      "id": "donor_user_id",
      "firstName": "John",
      "lastName": "Donor",
      "email": "donor@example.com"
    },
    "ngo": null
  }
]
```

#### GET /api/donations/{id}
Get specific donation by ID.

**Headers:**
- `Authorization: Bearer {jwt_token}`

#### POST /api/donations
Create new donation (Donor/Admin only).

**Request Body:**
```json
{
  "title": "Fresh Vegetables",
  "description": "Organic vegetables from local farm",
  "foodType": "Vegetables",
  "quantity": 50,
  "unit": "kg",
  "expiryDate": "2025-01-25T00:00:00Z",
  "pickupAddress": "123 Farm Road, Green Valley",
  "imageUrl": "https://example.com/image.jpg",
  "location": "Green Valley"
}
```

**Headers:**
- `Authorization: Bearer {jwt_token}`

#### PUT /api/donations/{id}
Update donation (Owner/Admin only).

**Request Body:**
```json
{
  "title": "Updated Title",
  "description": "Updated description",
  "foodType": "Fruits",
  "quantity": 30,
  "unit": "kg",
  "expiryDate": "2025-01-26T00:00:00Z",
  "pickupAddress": "456 New Address",
  "imageUrl": "https://example.com/new-image.jpg",
  "location": "New Location"
}
```

#### DELETE /api/donations/{id}
Delete donation (Owner/Admin only).

### Donation Request Endpoints

#### GET /api/donationrequests
Get donation requests based on user role.

**Query Parameters:**
- `page` (optional): Page number (default: 1)
- `pageSize` (optional): Items per page (default: 20)

**Role-based filtering:**
- **Donor:** Gets requests for their donations
- **NGO:** Gets their own requests
- **Admin:** Gets all requests

#### GET /api/donationrequests/{id}
Get specific donation request.

#### POST /api/donationrequests
Create donation request (NGO only).

**Request Body:**
```json
{
  "donationId": 1,
  "message": "We would like to request this donation for our food bank. We serve 500 families weekly."
}
```

#### PUT /api/donationrequests/{id}/approve
Approve donation request (Donor/Admin only).

**Request Body:**
```json
{
  "responseMessage": "Thank you for your interest. Please contact us to arrange pickup."
}
```

#### PUT /api/donationrequests/{id}/reject
Reject donation request (Donor/Admin only).

**Request Body:**
```json
{
  "responseMessage": "Sorry, this donation is no longer available."
}
```

#### DELETE /api/donationrequests/{id}
Delete donation request (NGO/Admin only).

### Notification Endpoints

#### GET /api/notifications
Get user notifications with pagination.

**Query Parameters:**
- `isRead` (optional): Filter by read status (true/false)
- `page` (optional): Page number (default: 1)
- `pageSize` (optional): Items per page (default: 20)

**Response:**
```json
{
  "notifications": [
    {
      "id": 1,
      "userId": "user_id",
      "title": "New Donation Request",
      "message": "Your donation 'Fresh Vegetables' has been requested by Food Bank Central.",
      "type": "Info",
      "timestamp": "2025-01-21T10:00:00Z",
      "isRead": false,
      "readAt": null,
      "actionUrl": "/donations/1",
      "relatedEntityId": 1,
      "relatedEntityType": "Donation"
    }
  ],
  "totalCount": 10,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

#### GET /api/notifications/{id}
Get specific notification.

#### PUT /api/notifications/{id}/read
Mark notification as read.

#### PUT /api/notifications/{id}/unread
Mark notification as unread.

#### PUT /api/notifications/mark-all-read
Mark all notifications as read.

#### DELETE /api/notifications/{id}
Delete specific notification.

#### DELETE /api/notifications/clear-all
Clear all notifications.

#### GET /api/notifications/stats
Get notification statistics.

**Response:**
```json
{
  "total": 25,
  "unread": 5,
  "read": 20,
  "today": 3
}
```

## Role-Based Access Control

### Donor Role
- **Can:** Create, read, update, delete own donations
- **Can:** View and respond to donation requests for their donations
- **Cannot:** Access other users' donations or requests

### NGO Role
- **Can:** View available donations
- **Can:** Create donation requests
- **Can:** View and manage own requests
- **Cannot:** Create or modify donations

### Admin Role
- **Can:** Access all endpoints
- **Can:** Manage all donations and requests
- **Can:** View system analytics
- **Can:** Manage users and roles

## Error Responses

### 400 Bad Request
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Title": ["The Title field is required."]
  }
}
```

### 401 Unauthorized
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401
}
```

### 403 Forbidden
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403
}
```

### 404 Not Found
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404
}
```

## Rate Limiting

- **Authentication endpoints:** 5 requests per minute
- **CRUD operations:** 100 requests per hour
- **Notification endpoints:** 200 requests per hour

## Pagination

Most list endpoints support pagination:

```
GET /api/donations?page=1&pageSize=10
```

**Response includes:**
- `totalCount`: Total number of items
- `page`: Current page number
- `pageSize`: Items per page
- `totalPages`: Total number of pages

## Filtering and Sorting

### Donations
- Filter by `category` (food type)
- Filter by `status`
- Sort by creation date (default: newest first)

### Notifications
- Filter by `isRead` status
- Sort by timestamp (default: newest first)

## Webhooks (Future Enhancement)

The system will support webhooks for real-time notifications:

- `donation.created`
- `donation.updated`
- `donation.deleted`
- `request.created`
- `request.approved`
- `request.rejected`

## SDK Examples

### JavaScript/TypeScript
```javascript
const api = {
  baseUrl: 'https://localhost:5001/api',
  token: 'your_jwt_token',
  
  async getDonations() {
    const response = await fetch(`${this.baseUrl}/donations`, {
      headers: {
        'Authorization': `Bearer ${this.token}`,
        'Content-Type': 'application/json'
      }
    });
    return response.json();
  },
  
  async createDonation(donation) {
    const response = await fetch(`${this.baseUrl}/donations`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${this.token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(donation)
    });
    return response.json();
  }
};
```

### C# .NET
```csharp
public class DonationApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "https://localhost:5001/api";
    
    public DonationApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<List<Donation>> GetDonationsAsync()
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/donations");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<Donation>>();
    }
    
    public async Task<Donation> CreateDonationAsync(CreateDonationDto donation)
    {
        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/donations", donation);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Donation>();
    }
}
```
