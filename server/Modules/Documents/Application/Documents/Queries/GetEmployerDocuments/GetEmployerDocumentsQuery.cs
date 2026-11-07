using MediatR;
using Normora.Modules.Documents.Application.Documents.DTOs;

namespace Normora.Modules.Documents.Application.Documents.Queries;

/// <summary>
/// Query to retrieve all active knowledge base documents belonging to the currently active tenant organization.
/// Handled by <see cref="GetEmployerDocumentsQueryHandler"/>.
/// </summary>
/// <remarks>
/// Management View:
/// - Scoped to ambient TenantId via ITenantContext.
/// - Excludes soft-deleted documents (DeletedAt != null).
/// - Includes version histories, processing statuses (Uploaded, Processing, Indexed, Failed), and departmental tags.
/// </remarks>
public record GetEmployerDocumentsQuery : IRequest<List<DocumentDto>>;

