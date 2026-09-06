using System;
using System.Collections.Generic;
using System.Linq;
using Normora.Shared;

namespace Normora.Api.Features.Documents;

public record DocumentDto(
    Guid Id,
    string FileName,
    string Status,
    DateTime UploadedAt,
    IReadOnlyCollection<Guid> DepartmentIds);

public static class DocumentExtensions
{
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
