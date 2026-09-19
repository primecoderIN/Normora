using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Normora.Api.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // Extract the user's email from their JWT claims to identify them across connections
        var email = Context.User?.Claims.FirstOrDefault(c => c.Type == "email" || c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
        if (!string.IsNullOrEmpty(email))
        {
            // Add the user's SignalR connection to a group named after their email address so we can easily target notifications to them
            await Groups.AddToGroupAsync(Context.ConnectionId, email.ToLowerInvariant());
        }

        await base.OnConnectedAsync();
    }
}
