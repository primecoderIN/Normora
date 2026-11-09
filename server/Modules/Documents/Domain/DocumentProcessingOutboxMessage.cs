using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Domain;

public sealed class DocumentProcessingOutboxMessage : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid DocumentVersionId { get; set; }
    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
