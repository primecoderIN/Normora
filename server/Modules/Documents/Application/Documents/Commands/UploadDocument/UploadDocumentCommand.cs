using MediatR;
using Microsoft.AspNetCore.Http;
using Normora.Modules.Documents.Application.Documents.DTOs;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Application.Documents.Commands;

/// <summary>
/// Command to upload a new document or a new version of an existing document.
/// </summary>
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

