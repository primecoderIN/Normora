using System;
using System.Collections.Generic;
using System.Linq;
using Normora.Shared;

namespace Normora.Api.Features.Documents;

/// <summary>
/// Data transfer object representing a document for API responses.
/// Decouples EF Core entities from the API surface.
/// </summary>
public record DocumentDto(
    Guid Id,
    string FileName,
    string Status,
    DateTime UploadedAt,
    IReadOnlyCollection<Guid> DepartmentIds,
    // Document Versioning (Phase 19): Includes all historical file versions.
    IReadOnlyCollection<DocumentVersionDto> Versions);

public record DocumentVersionDto(
    Guid Id,
    int VersionNumber,
    string Status,
    bool IsActive,
    DateTime CreatedAt);

/// <summary>
/// Extension methods for mapping Document entities to DTOs.
/// </summary>
public static class DocumentExtensions
{
    /// <summary>
    /// Maps a <see cref="Document"/> entity to a <see cref="DocumentDto"/>.
    /// </summary>
    public static DocumentDto ToDto(this Document document)
    {
        var activeVersion = document.Versions.FirstOrDefault(v => v.IsActive) 
                            ?? document.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
                            
        return new DocumentDto(
            document.Id,
            document.FileName,
            activeVersion?.Status.ToString() ?? "Unknown",
            document.UploadedAt,
            document.DocumentDepartments?.Select(d => d.DepartmentId).ToList() ?? new List<Guid>(),
            document.Versions?.Select(v => new DocumentVersionDto(
                v.Id,
                v.VersionNumber,
                v.Status.ToString(),
                v.IsActive,
                v.CreatedAt)).ToList() ?? new List<DocumentVersionDto>()
        );
    }
}
