using MediatR;
using Microsoft.AspNetCore.Http;
using Normora.Modules.Documents.Application.Documents.DTOs;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Application.Documents.Commands;

/// <summary>
/// Command to upload a new document or a new version of an existing document into the organization's knowledge base.
/// Handled by <see cref="UploadDocumentCommandHandler"/>.
/// </summary>
/// <remarks>
/// Ingestion Pipeline Workflow:
/// - Enforces subscription quotas (document limits and storage bytes) prior to accepting upload.
/// - Performs deep magic byte inspection via FluentValidation to guarantee authenticity (PDF, DOCX, TXT, CSV).
/// - Persists binary blob to MinIO under secure object keys ({tenantId}/{docId}.pdf).
/// - Persists a processing outbox message for asynchronous text extraction, chunking, and embedding.
/// - Emits real-time progress events over SignalR to update the employer processing status indicator.
/// - Implements <see cref="IAuditableRequest"/> to trace document uploads.
/// </remarks>
/// <param name="File">The multipart form file containing the uploaded document stream.</param>
/// <param name="TenantId">Target organization identifier.</param>
/// <param name="DepartmentIds">Optional collection of department scopes (empty/null implies Company-Wide).</param>
/// <param name="DocumentId">Optional parent DocumentId when creating a new revision of an existing document.</param>
public record UploadDocumentCommand(
    IFormFile File,
    Guid TenantId,
    IReadOnlyCollection<Guid>? DepartmentIds = null,
    Guid? DocumentId = null) : IRequest<DocumentDto>, IAuditableRequest
{
    public string Action => DocumentId.HasValue ? "Document.VersionCreated" : "Document.Uploaded";
    public string TargetType => "Document";
    public string? GetTargetId(object? response) => response is DocumentDto doc ? doc.Id.ToString() : DocumentId?.ToString();
    public string? GetMetadata(object? response) => response is DocumentDto doc
        ? System.Text.Json.JsonSerializer.Serialize(new { fileName = doc.FileName, status = doc.Status, versionCount = doc.Versions.Count })
        : System.Text.Json.JsonSerializer.Serialize(new { fileName = File?.FileName, fileSize = File?.Length });
}
