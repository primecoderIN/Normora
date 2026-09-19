using Microsoft.AspNetCore.SignalR;
using Normora.Api.Hubs;
using Normora.Shared.Interfaces;

namespace Normora.Api.Services;

public class NotificationService(IHubContext<NotificationHub> hubContext) : INotificationService
{
    public async Task NotifyInvitationReceivedAsync(string email, string tenantName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email)) return;

        // Target the specific user's connection by their email address to send a real-time invitation notification
        var targetEmail = email.ToLowerInvariant();
        await hubContext.Clients.Group(targetEmail).SendAsync("ReceiveInvitation", new
        {
            TenantName = tenantName,
            Timestamp = DateTimeOffset.UtcNow
        }, cancellationToken);
    }
}
