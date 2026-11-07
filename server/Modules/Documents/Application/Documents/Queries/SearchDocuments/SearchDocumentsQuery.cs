using MediatR;
using Normora.Modules.Documents.Application.Documents.DTOs;

namespace Normora.Modules.Documents.Application.Documents.Queries;

/// <summary>
/// Query to perform a dense vector similarity search across indexed document chunks within the tenant's knowledge base.
/// Handled by <see cref="SearchDocumentsQueryHandler"/>.
/// </summary>
/// <remarks>
/// Vector Retrieval Architecture:
/// - Generates query embedding using Google Gemini embedding models (768 dimensions).
/// - Queries pgvector using cosine distance operator (<=>) backed by HNSW indexing.
/// - Enforces departmental ACL scoping: checks caller's memberships to exclude unauthorized chunks.
/// </remarks>
/// <param name="Query">The search inquiry or question text to embed.</param>
/// <param name="Limit">Maximum number of nearest neighbor chunks to return (default: 5).</param>
public sealed record SearchDocumentsQuery(string Query, int Limit = 5) : IRequest<IReadOnlyList<DocumentSearchResult>>;

