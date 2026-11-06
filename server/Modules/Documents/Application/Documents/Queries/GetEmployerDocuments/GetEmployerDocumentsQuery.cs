using MediatR;
using Normora.Modules.Documents.Application.Documents.DTOs;

namespace Normora.Modules.Documents.Application.Documents.Queries;

/// <summary>
/// A query to retrieve all documents belonging to the currently active tenant.
/// </summary>
public record GetEmployerDocumentsQuery : IRequest<List<DocumentDto>>;

