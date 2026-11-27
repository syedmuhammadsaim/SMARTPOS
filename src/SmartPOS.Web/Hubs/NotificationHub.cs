using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SmartPOS.Web.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var businessId = Context.User?.FindFirst("BusinessId")?.Value;
        if (!string.IsNullOrEmpty(businessId))
        {
            // Join tenant-specific SignalR group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Tenant_{businessId}");
        }

        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            // Join user-specific SignalR group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var businessId = Context.User?.FindFirst("BusinessId")?.Value;
        if (!string.IsNullOrEmpty(businessId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Tenant_{businessId}");
        }

        await base.OnDisconnectedAsync(exception);
    }
}
