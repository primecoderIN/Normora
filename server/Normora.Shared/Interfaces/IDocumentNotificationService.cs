namespace Normora.Shared.Interfaces;

/// <summary>
/// Abstraction for broadcasting real-time document processing and lifecycle notifications to clients.
/// Decouples module application logic from specific transport implementations (such as SignalR).
/// </summary>
public interface IDocumentNotificationService
{
    /// <summary>
    /// Broadcasts a status update when a document transitions between processing states.
    /// </summary>
    Task SendDocumentStatusChangedAsync(
        Guid tenantId,
        Guid documentId,
        string fileName,
        string status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts an event when a document is soft-deleted.
    /// </summary>
    Task SendDocumentDeletedAsync(
        Guid tenantId,
        Guid documentId,
        CancellationToken cancellationToken = default);
}
