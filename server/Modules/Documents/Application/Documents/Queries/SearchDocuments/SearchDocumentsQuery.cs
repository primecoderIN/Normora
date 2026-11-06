using MediatR;
using Normora.Modules.Documents.Application.Documents.DTOs;

namespace Normora.Modules.Documents.Application.Documents.Queries;

/// <summary>
/// Query to perform a semantic (vector) similarity search across all indexed document chunks for the current tenant.
/// </summary>
/// <param name="Query">The search text to embed and compare against stored chunk embeddings.</param>
/// <param name="Limit">Maximum number of results to return (1–20).</param>
public sealed record SearchDocumentsQuery(string Query, int Limit = 5) : IRequest<IReadOnlyList<DocumentSearchResult>>;

