using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SmartDonationSystem.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task JoinUserGroup(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        }

        public async Task JoinRoleGroup(string role)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Role_{role}");
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            }

            if (!string.IsNullOrEmpty(userRole))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Role_{userRole}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
            }

            if (!string.IsNullOrEmpty(userRole))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Role_{userRole}");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
