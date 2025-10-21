# Smart Donation System - Role-Based Authentication Guide

## Overview

The Smart Donation System implements comprehensive role-based authentication with three distinct user roles, each with specific permissions and functionality.

## User Roles

### 1. **Donor Role**
**Purpose:** Individuals or organizations who want to donate food items

**Permissions:**
- Create and manage their own donations
- View all donations
- Edit/delete their own donations
- View donation details

**Key Features:**
- Create new donations with detailed information
- Track donation status (Available, Reserved, Collected, Expired)
- Manage donation details (title, description, food type, quantity, pickup address)
- View analytics of their donations

**Navigation:**
- Home → Create Donation
- View All Donations
- My Donations (filtered view)

### 2. **NGO Role**
**Purpose:** Non-profit organizations that collect and distribute donated food

**Permissions:**
- View all available donations
- Request donations from donors
- Manage their donation requests
- View their NGO profile and statistics

**Key Features:**
- Browse available donations
- Submit donation requests with messages to donors
- Track request status (Pending, Approved, Rejected, Completed)
- View organization dashboard with statistics
- Manage NGO profile information

**Navigation:**
- NGO Dashboard
- Available Donations
- My Requests
- Organization Profile

### 3. **Admin Role**
**Purpose:** System administrators with full access to manage the platform

**Permissions:**
- View system analytics and statistics
- Manage all users (view, edit roles, delete)
- Manage all donations (view, delete)
- Manage NGO organizations
- Access comprehensive dashboard

**Key Features:**
- System-wide analytics dashboard
- User management (role changes, user deletion)
- Donation management
- NGO organization oversight
- System statistics and reporting

**Navigation:**
- Admin Dashboard
- Manage Users
- Manage Donations
- Manage NGOs
- System Analytics

## Authentication Flow

### Registration Process
1. User registers with basic information (name, email, password)
2. User selects their role (Donor, NGO, Admin)
3. System creates user account with selected role
4. For NGO users, additional NGO profile is created
5. User receives confirmation and can log in

### Login Process
1. User enters email and password
2. System validates credentials
3. JWT token is generated with role information
4. User is redirected to role-appropriate dashboard

### Role-Based Redirects
- **Donor:** Redirected to donations list with "Create Donation" option
- **NGO:** Redirected to NGO dashboard with available donations
- **Admin:** Redirected to admin dashboard with system analytics

## Database Seeding

The system automatically seeds the database with:

### Default Roles
- `Donor` - For individual donors
- `NGO` - For non-profit organizations  
- `Admin` - For system administrators

### Sample Users
1. **Admin User**
   - Email: `admin@smartdonation.com`
   - Password: `Admin@123`
   - Role: Admin
   - Full system access

2. **Sample Donor**
   - Email: `donor@example.com`
   - Password: `Donor@123`
   - Role: Donor
   - Has sample donations

3. **Sample NGO**
   - Email: `ngo@example.com`
   - Password: `NGO@123`
   - Role: NGO
   - Has NGO profile and can request donations

### Sample Data
- 3 sample donations with different food types
- 1 NGO organization profile
- User accounts with proper role assignments

## Security Features

### JWT Authentication
- Secure token-based authentication for API endpoints
- Token includes user ID, email, name, and roles
- Configurable token expiration (60 minutes default)
- Secure secret key for token signing

### Role-Based Authorization
- Controller-level authorization attributes
- Action-level role requirements
- View-level role-based content display
- API endpoint protection

### Password Security
- Minimum 6 characters
- Requires uppercase, lowercase, digit, and special character
- Secure password hashing with ASP.NET Identity

## API Endpoints by Role

### Public Endpoints
- `POST /api/auth/register` - User registration
- `POST /api/auth/login` - User login
- `POST /api/auth/logout` - User logout

### Donor Endpoints
- `GET /api/donations` - View all donations
- `POST /api/donations` - Create donation
- `PUT /api/donations/{id}` - Update own donation
- `DELETE /api/donations/{id}` - Delete own donation

### NGO Endpoints
- `GET /api/donations` - View available donations
- `GET /api/donations/{id}` - View donation details
- `POST /api/donation-requests` - Request donation

### Admin Endpoints
- All donor and NGO endpoints
- `GET /api/admin/users` - Manage users
- `GET /api/admin/analytics` - System analytics
- `DELETE /api/admin/users/{id}` - Delete users

## Navigation Structure

### Role-Based Menu Items
The navigation automatically shows/hides menu items based on user role:

**All Users:**
- Home
- Donations (view all)
- Privacy

**Donor Only:**
- Create Donation

**NGO Only:**
- NGO Dashboard
- Available Donations
- My Requests

**Admin Only:**
- Admin Dashboard
- Manage Users
- Manage Donations
- Manage NGOs

## Testing the System

### Test Accounts
Use these accounts to test different roles:

1. **Admin Testing:**
   ```
   Email: admin@smartdonation.com
   Password: Admin@123
   ```

2. **Donor Testing:**
   ```
   Email: donor@example.com
   Password: Donor@123
   ```

3. **NGO Testing:**
   ```
   Email: ngo@example.com
   Password: NGO@123
   ```

### Test Scenarios
1. **Donor Workflow:**
   - Login as donor
   - Create a new donation
   - View all donations
   - Edit own donation

2. **NGO Workflow:**
   - Login as NGO
   - Browse available donations
   - Request a donation
   - View request status

3. **Admin Workflow:**
   - Login as admin
   - View system analytics
   - Manage users
   - View all donations

## Implementation Details

### Controllers
- `AdminController` - Admin-specific functionality
- `NGOController` - NGO-specific functionality  
- `DonationsController` - Donation management (all roles)
- `AuthController` - Authentication endpoints

### Views
- Role-specific dashboards
- Role-based navigation
- Conditional content display
- Responsive design for all devices

### Models
- `ApplicationUser` - Extended Identity user
- `NGO` - NGO organization profiles
- `Donation` - Food donation items
- `DonationRequest` - NGO requests for donations
- `Notification` - User notifications

## Security Best Practices

1. **Role Validation:** All controllers validate user roles before allowing access
2. **Data Isolation:** Users can only access their own data (except admins)
3. **Secure Tokens:** JWT tokens include role information for API authorization
4. **Input Validation:** All forms include proper validation and sanitization
5. **CSRF Protection:** All forms include anti-forgery tokens

## Future Enhancements

- Email notifications for donation requests
- Real-time chat between donors and NGOs
- Advanced analytics and reporting
- Mobile app integration
- Push notifications
- Donation tracking and delivery confirmation
