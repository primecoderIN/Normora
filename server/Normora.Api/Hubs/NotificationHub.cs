using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Normora.Api.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    // The Hub uses the default UserIdentifierProvider which extracts the "sub" claim (Keycloak User ID)
    // or we can map it by email if we add a custom IUserIdProvider.
    // Actually, invitations are sent by email, but we want to notify the user if they are logged in.
    // The default IUserIdProvider in ASP.NET Core uses ClaimTypes.NameIdentifier which maps to "sub".
    // We will just use standard Groups to map emails to connections to make it robust,
    // since the NotificationService might only know the email address of the invitee.

    public override async Task OnConnectedAsync()
    {
        var email = Context.User?.Claims.FirstOrDefault(c => c.Type == "email" || c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
        if (!string.IsNullOrEmpty(email))
        {
            // Add the connection to a group named after their email address so we can target them
            await Groups.AddToGroupAsync(Context.ConnectionId, email.ToLowerInvariant());
        }

        await base.OnConnectedAsync();
    }
}
