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
    IReadOnlyCollection<Guid> DepartmentIds);

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
        return new DocumentDto(
            document.Id,
            document.FileName,
            document.Status.ToString(),
            document.UploadedAt,
            document.DocumentDepartments?.Select(d => d.DepartmentId).ToList() ?? new List<Guid>()
        );
    }
}
