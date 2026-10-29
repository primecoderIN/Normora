using Microsoft.AspNetCore.SignalR;
using Normora.Api.Hubs;
using Normora.Shared.Interfaces;

namespace Normora.Api.Services;

/// <summary>
/// Implements IDocumentNotificationService using SignalR to broadcast real-time events to connected clients.
/// </summary>
public sealed class SignalRDocumentNotificationService(IHubContext<DocumentHub> hubContext) : IDocumentNotificationService
{
    public async Task SendDocumentStatusChangedAsync(
        Guid tenantId,
        Guid documentId,
        string fileName,
        string status,
        CancellationToken cancellationToken = default)
    {
        await hubContext.Clients
            .Group(DocumentHub.GroupName(tenantId))
            .SendAsync("DocumentStatusChanged", new DocumentStatusChanged(documentId, tenantId, fileName, status), cancellationToken);
    }

    public async Task SendDocumentDeletedAsync(
        Guid tenantId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        await hubContext.Clients
            .Group(DocumentHub.GroupName(tenantId))
            .SendAsync("DocumentDeleted", documentId, cancellationToken);
    }
}
