# Smart Donation System - API Endpoints

## Authentication Endpoints

### POST /api/auth/register
Register a new user with role (Donor, NGO, or Admin)

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

### POST /api/auth/login
Login with email and password

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

### POST /api/auth/logout
Logout (requires authentication)

### GET /api/auth/profile
Get current user profile (requires authentication)

## Donations Endpoints

### GET /api/donations
Get all donations with optional filtering

**Query Parameters:**
- `category` (optional): Filter by category
- `status` (optional): Filter by status

**Headers:**
- `Authorization: Bearer {jwt_token}`

### GET /api/donations/{id}
Get specific donation by ID

**Headers:**
- `Authorization: Bearer {jwt_token}`

### POST /api/donations
Create new donation (Donor/Admin only)

**Request Body:**
```json
{
  "title": "Fresh Vegetables",
  "description": "Fresh organic vegetables from local farm",
  "category": "Food",
  "quantity": 50,
  "unit": "kg",
  "expiryDate": "2025-01-25T00:00:00Z",
  "imageUrl": "https://example.com/image.jpg",
  "location": "Downtown"
}
```

**Headers:**
- `Authorization: Bearer {jwt_token}`

### PUT /api/donations/{id}
Update donation (Owner/Admin only)

### DELETE /api/donations/{id}
Delete donation (Owner/Admin only)

## Default Admin Account

The system creates a default admin account:
- **Email:** admin@smartdonation.com
- **Password:** Admin@123
- **Role:** Admin

## JWT Token Usage

Include the JWT token in the Authorization header for protected endpoints:
```
Authorization: Bearer {your_jwt_token}
```

## Roles

- **Donor:** Can create, edit, and delete their own donations
- **NGO:** Can view donations and request them
- **Admin:** Full access to all operations
