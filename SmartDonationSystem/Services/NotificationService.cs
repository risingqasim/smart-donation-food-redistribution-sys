using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SmartDonationSystem.Data;
using SmartDonationSystem.Hubs;
using SmartDonationSystem.Models;
using System.Security.Claims;

namespace SmartDonationSystem.Services
{
    public class NotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ApplicationDbContext _context;

        public NotificationService(IHubContext<NotificationHub> hubContext, ApplicationDbContext context)
        {
            _hubContext = hubContext;
            _context = context;
        }

        public async Task NotifyNewDonationAsync(Donation donation)
        {
            try
            {
                // Get all NGO users
                var ngoUsers = await _context.Users
                    .Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && 
                        _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "NGO")))
                    .AsNoTracking()
                    .ToListAsync();

                var notification = new DonationNotification
                {
                    DonationId = donation.Id,
                    Title = "New Donation Available",
                    Description = donation.Description,
                    FoodType = donation.FoodType,
                    Quantity = donation.Quantity,
                    Unit = donation.Unit ?? "",
                    ExpiryDate = donation.ExpiryDate,
                    PickupAddress = donation.PickupAddress,
                    DonorName = $"{donation.Donor?.FirstName} {donation.Donor?.LastName}",
                    DonorContact = donation.Donor?.Email ?? "",
                    Latitude = donation.Donor?.Latitude,
                    Longitude = donation.Donor?.Longitude,
                    CreatedAt = donation.CreatedAt
                };

                var message = new NotificationMessage
                {
                    Title = "New Donation Available",
                    Message = $"{donation.FoodType} ({donation.Quantity} {donation.Unit}) - {donation.Title}",
                    Type = "info",
                    Icon = "fas fa-gift",
                    ActionUrl = $"/Donations/Details/{donation.Id}",
                    RelatedEntityType = "Donation",
                    RelatedEntityId = donation.Id,
                    SenderName = "System"
                };

                // Notify all NGO users
                await _hubContext.Clients.Group("Role_NGO").SendAsync("ReceiveNotification", message);
                await _hubContext.Clients.Group("Role_NGO").SendAsync("ReceiveDonationNotification", notification);

                // Save notification to database for each NGO user
                foreach (var ngoUser in ngoUsers)
                {
                    var dbNotification = new Notification
                    {
                        UserId = ngoUser.Id,
                        Title = message.Title,
                        Message = message.Message,
                        Type = "DonationUpdate",
                        ActionUrl = message.ActionUrl,
                        RelatedEntityType = "Donation",
                        RelatedEntityId = donation.Id,
                        Timestamp = DateTime.UtcNow,
                        IsRead = false
                    };

                    _context.Notifications.Add(dbNotification);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error notifying new donation: {ex.Message}");
            }
        }

        public async Task NotifyDonationStatusUpdateAsync(Donation donation, string oldStatus, string newStatus)
        {
            try
            {
                var message = new NotificationMessage
                {
                    Title = "Donation Status Updated",
                    Message = $"Your donation '{donation.Title}' status changed from {oldStatus} to {newStatus}",
                    Type = "info",
                    Icon = "fas fa-info-circle",
                    ActionUrl = $"/Donations/Details/{donation.Id}",
                    RelatedEntityType = "Donation",
                    RelatedEntityId = donation.Id,
                    SenderName = "System"
                };

                // Notify the donor
                if (!string.IsNullOrEmpty(donation.DonorId))
                {
                    await _hubContext.Clients.Group($"User_{donation.DonorId}").SendAsync("ReceiveNotification", message);
                }

                // Save notification to database
                if (!string.IsNullOrEmpty(donation.DonorId))
                {
                    var dbNotification = new Notification
                    {
                        UserId = donation.DonorId,
                        Title = message.Title,
                        Message = message.Message,
                        Type = "DonationUpdate",
                        ActionUrl = message.ActionUrl,
                        RelatedEntityType = "Donation",
                        RelatedEntityId = donation.Id,
                        Timestamp = DateTime.UtcNow,
                        IsRead = false
                    };

                    _context.Notifications.Add(dbNotification);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error notifying donation status update: {ex.Message}");
            }
        }

        public async Task NotifyDonationRequestAsync(DonationRequest request)
        {
            try
            {
                var donation = await _context.Donations
                    .Include(d => d.Donor)
                    .FirstOrDefaultAsync(d => d.Id == request.DonationId);

                if (donation?.DonorId != null)
                {
                    var message = new NotificationMessage
                    {
                        Title = "New Donation Request",
                        Message = $"NGO '{request.NGO?.Name}' has requested your donation '{donation.Title}'",
                        Type = "info",
                        Icon = "fas fa-hand-paper",
                        ActionUrl = $"/Donor/DonationRequests",
                        RelatedEntityType = "DonationRequest",
                        RelatedEntityId = request.Id,
                        SenderName = request.NGO?.Name ?? "NGO"
                    };

                    // Notify the donor
                    await _hubContext.Clients.Group($"User_{donation.DonorId}").SendAsync("ReceiveNotification", message);

                    // Save notification to database
                    var dbNotification = new Notification
                    {
                        UserId = donation.DonorId,
                        Title = message.Title,
                        Message = message.Message,
                        Type = "RequestStatus",
                        ActionUrl = message.ActionUrl,
                        RelatedEntityType = "DonationRequest",
                        RelatedEntityId = request.Id,
                        Timestamp = DateTime.UtcNow,
                        IsRead = false
                    };

                    _context.Notifications.Add(dbNotification);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error notifying donation request: {ex.Message}");
            }
        }

        public async Task NotifyDonationRequestStatusUpdateAsync(DonationRequest request, string oldStatus, string newStatus)
        {
            try
            {
                var donation = await _context.Donations
                    .Include(d => d.Donor)
                    .FirstOrDefaultAsync(d => d.Id == request.DonationId);

                if (donation?.DonorId != null)
                {
                    var message = new NotificationMessage
                    {
                        Title = "Donation Request Status Updated",
                        Message = $"Your request for '{donation.Title}' has been {newStatus.ToLower()}",
                        Type = newStatus == "Approved" ? "success" : newStatus == "Rejected" ? "warning" : "info",
                        Icon = newStatus == "Approved" ? "fas fa-check-circle" : newStatus == "Rejected" ? "fas fa-times-circle" : "fas fa-info-circle",
                        ActionUrl = $"/Donor/DonationRequests",
                        RelatedEntityType = "DonationRequest",
                        RelatedEntityId = request.Id,
                        SenderName = "System"
                    };

                    // Notify the donor
                    await _hubContext.Clients.Group($"User_{donation.DonorId}").SendAsync("ReceiveNotification", message);

                    // Save notification to database
                    var dbNotification = new Notification
                    {
                        UserId = donation.DonorId,
                        Title = message.Title,
                        Message = message.Message,
                        Type = "RequestStatus",
                        ActionUrl = message.ActionUrl,
                        RelatedEntityType = "DonationRequest",
                        RelatedEntityId = request.Id,
                        Timestamp = DateTime.UtcNow,
                        IsRead = false
                    };

                    _context.Notifications.Add(dbNotification);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error notifying donation request status update: {ex.Message}");
            }
        }

        public async Task NotifyDonationPickedUpAsync(Donation donation)
        {
            try
            {
                if (!string.IsNullOrEmpty(donation.DonorId))
                {
                    var message = new NotificationMessage
                    {
                        Title = "Donation Picked Up",
                        Message = $"Your donation '{donation.Title}' has been successfully picked up",
                        Type = "success",
                        Icon = "fas fa-check-circle",
                        ActionUrl = $"/Donations/Details/{donation.Id}",
                        RelatedEntityType = "Donation",
                        RelatedEntityId = donation.Id,
                        SenderName = "System"
                    };

                    // Notify the donor
                    await _hubContext.Clients.Group($"User_{donation.DonorId}").SendAsync("ReceiveNotification", message);

                    // Save notification to database
                    var dbNotification = new Notification
                    {
                        UserId = donation.DonorId,
                        Title = message.Title,
                        Message = message.Message,
                        Type = "DonationUpdate",
                        ActionUrl = message.ActionUrl,
                        RelatedEntityType = "Donation",
                        RelatedEntityId = donation.Id,
                        Timestamp = DateTime.UtcNow,
                        IsRead = false
                    };

                    _context.Notifications.Add(dbNotification);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error notifying donation pickup: {ex.Message}");
            }
        }

        public async Task SendSystemNotificationAsync(string title, string message, string type = "info", string? targetUserId = null, string? targetRole = null)
        {
            try
            {
                var notification = new NotificationMessage
                {
                    Title = title,
                    Message = message,
                    Type = type,
                    Icon = type switch
                    {
                        "success" => "fas fa-check-circle",
                        "warning" => "fas fa-exclamation-triangle",
                        "error" => "fas fa-exclamation-circle",
                        _ => "fas fa-info-circle"
                    },
                    SenderName = "System"
                };

                if (!string.IsNullOrEmpty(targetUserId))
                {
                    await _hubContext.Clients.Group($"User_{targetUserId}").SendAsync("ReceiveNotification", notification);
                }
                else if (!string.IsNullOrEmpty(targetRole))
                {
                    await _hubContext.Clients.Group($"Role_{targetRole}").SendAsync("ReceiveNotification", notification);
                }
                else
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending system notification: {ex.Message}");
            }
        }

        public async Task MarkNotificationAsReadAsync(string userId, int notificationId)
        {
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

                if (notification != null)
                {
                    notification.IsRead = true;
                    await _context.SaveChangesAsync();

                    // Notify client that notification was marked as read
                    await _hubContext.Clients.Group($"User_{userId}").SendAsync("NotificationMarkedAsRead", notificationId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking notification as read: {ex.Message}");
            }
        }

        public async Task GetUnreadNotificationCountAsync(string userId)
        {
            try
            {
                var count = await _context.Notifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead);

                await _hubContext.Clients.Group($"User_{userId}").SendAsync("UpdateNotificationCount", count);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting unread notification count: {ex.Message}");
            }
        }
    }
}
