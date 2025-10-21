# Smart Donation System - EF Core Models Documentation

## Model Overview

The Smart Donation System uses Entity Framework Core with SQL Server and includes the following models:

### 1. ApplicationUser (Identity)
**Inherits from:** `IdentityUser`

**Properties:**
- `FirstName` (string, required, max 100)
- `LastName` (string, required, max 100)
- `Address` (string, optional, max 500)
- `City` (string, optional, max 50)
- `PostalCode` (string, optional, max 20)
- `State` (string, optional, max 50)
- `Country` (string, optional, max 50)
- `CreatedAt` (DateTime, default: UtcNow)
- `UpdatedAt` (DateTime?, nullable)

**Navigation Properties:**
- `Donations` (ICollection<Donation>) - One-to-Many
- `Notifications` (ICollection<Notification>) - One-to-Many
- `NGO` (NGO?) - One-to-One

### 2. NGO
**Primary Key:** `Id` (int)

**Properties:**
- `Name` (string, required, max 200)
- `Location` (string, required, max 500)
- `Contact` (string, required, max 100)
- `Capacity` (int, required)
- `Description` (string, optional, max 1000)
- `Website` (string, optional, max 200)
- `RegistrationNumber` (string, optional, max 50)
- `CreatedAt` (DateTime, default: UtcNow)
- `UpdatedAt` (DateTime?, nullable)
- `UserId` (string, optional) - Foreign Key to ApplicationUser

**Navigation Properties:**
- `User` (ApplicationUser?) - One-to-One
- `Donations` (ICollection<Donation>) - One-to-Many
- `DonationRequests` (ICollection<DonationRequest>) - One-to-Many

### 3. Donation
**Primary Key:** `Id` (int)

**Properties:**
- `Title` (string, required, max 200)
- `Description` (string, required, max 1000)
- `FoodType` (string, required, max 50) - Vegetables, Fruits, Grains, Dairy, etc.
- `Status` (string, required, max 20) - Available, Reserved, Collected, Expired
- `Quantity` (int, required)
- `Unit` (string, optional, max 50) - pieces, kg, liters, etc.
- `ExpiryDate` (DateTime)
- `PickupAddress` (string, required, max 500)
- `ImageUrl` (string, optional, max 500)
- `Location` (string, optional, max 200)
- `DonorId` (string, required) - Foreign Key to ApplicationUser
- `NGOId` (int?, optional) - Foreign Key to NGO
- `CreatedAt` (DateTime, default: UtcNow)
- `UpdatedAt` (DateTime?, nullable)
- `CollectedAt` (DateTime?, nullable)

**Navigation Properties:**
- `Donor` (ApplicationUser?) - Many-to-One
- `NGO` (NGO?) - Many-to-One
- `DonationRequests` (ICollection<DonationRequest>) - One-to-Many

### 4. DonationRequest
**Primary Key:** `Id` (int)

**Properties:**
- `DonationId` (int, required) - Foreign Key to Donation
- `NGOId` (int, required) - Foreign Key to NGO
- `Message` (string, required, max 500)
- `Status` (string, required, max 20) - Pending, Approved, Rejected, Completed
- `ResponseMessage` (string, optional, max 200)
- `CreatedAt` (DateTime, default: UtcNow)
- `UpdatedAt` (DateTime?, nullable)
- `RespondedAt` (DateTime?, nullable)

**Navigation Properties:**
- `Donation` (Donation?) - Many-to-One
- `NGO` (NGO?) - Many-to-One

### 5. Notification
**Primary Key:** `Id` (int)

**Properties:**
- `UserId` (string, required) - Foreign Key to ApplicationUser
- `Message` (string, required, max 500)
- `Title` (string, optional, max 200)
- `Type` (string, optional, max 50) - Info, Warning, Success, Error
- `Timestamp` (DateTime, default: UtcNow)
- `IsRead` (bool, default: false)
- `ReadAt` (DateTime?, nullable)
- `ActionUrl` (string, optional, max 200)
- `RelatedEntityId` (int?, optional) - For linking to donations, requests, etc.
- `RelatedEntityType` (string, optional, max 50) - Donation, DonationRequest, etc.

**Navigation Properties:**
- `User` (ApplicationUser?) - Many-to-One

**Indexes:**
- Composite index on `UserId` and `IsRead`
- Index on `Timestamp`

## Relationships

### 1. ApplicationUser ↔ NGO
- **Type:** One-to-One
- **Description:** Each user can be associated with one NGO, and each NGO belongs to one user
- **Foreign Key:** `NGO.UserId` → `ApplicationUser.Id`
- **Delete Behavior:** Cascade (if user is deleted, NGO is deleted)

### 2. ApplicationUser ↔ Donation
- **Type:** One-to-Many
- **Description:** A user can create multiple donations, each donation has one donor
- **Foreign Key:** `Donation.DonorId` → `ApplicationUser.Id`
- **Delete Behavior:** Restrict (cannot delete user if they have donations)

### 3. NGO ↔ Donation
- **Type:** One-to-Many
- **Description:** An NGO can collect multiple donations, each donation can be reserved by one NGO
- **Foreign Key:** `Donation.NGOId` → `NGO.Id`
- **Delete Behavior:** SetNull (if NGO is deleted, donation.NGOId becomes null)

### 4. Donation ↔ DonationRequest
- **Type:** One-to-Many
- **Description:** A donation can have multiple requests from different NGOs
- **Foreign Key:** `DonationRequest.DonationId` → `Donation.Id`
- **Delete Behavior:** Cascade (if donation is deleted, requests are deleted)

### 5. NGO ↔ DonationRequest
- **Type:** One-to-Many
- **Description:** An NGO can make multiple requests for different donations
- **Foreign Key:** `DonationRequest.NGOId` → `NGO.Id`
- **Delete Behavior:** Restrict (cannot delete NGO if they have requests)

### 6. ApplicationUser ↔ Notification
- **Type:** One-to-Many
- **Description:** A user can have multiple notifications
- **Foreign Key:** `Notification.UserId` → `ApplicationUser.Id`
- **Delete Behavior:** Cascade (if user is deleted, notifications are deleted)

## Database Schema Features

### Constraints
- All required fields have `NOT NULL` constraints
- String fields have appropriate length limits
- Foreign key constraints ensure referential integrity

### Indexes
- Primary keys are automatically indexed
- Notifications have composite index on `UserId` and `IsRead` for efficient querying
- Notifications have index on `Timestamp` for chronological queries

### Delete Behaviors
- **Cascade:** Deleting parent deletes children (User → NGO, User → Notifications)
- **Restrict:** Prevents deletion if children exist (User → Donations, NGO → DonationRequests)
- **SetNull:** Sets foreign key to null when parent is deleted (NGO → Donations)

## Usage Examples

### Creating a Donation
```csharp
var donation = new Donation
{
    Title = "Fresh Vegetables",
    Description = "Organic vegetables from local farm",
    FoodType = "Vegetables",
    Quantity = 50,
    Unit = "kg",
    ExpiryDate = DateTime.Now.AddDays(3),
    PickupAddress = "123 Main St, City",
    DonorId = userId
};
```

### Creating an NGO
```csharp
var ngo = new NGO
{
    Name = "Food Bank Central",
    Location = "456 Oak Ave, Downtown",
    Contact = "contact@foodbank.org",
    Capacity = 1000,
    UserId = userId
};
```

### Creating a Notification
```csharp
var notification = new Notification
{
    UserId = userId,
    Title = "New Donation Available",
    Message = "Fresh vegetables are available for pickup",
    Type = "Info",
    RelatedEntityId = donationId,
    RelatedEntityType = "Donation"
};
```
