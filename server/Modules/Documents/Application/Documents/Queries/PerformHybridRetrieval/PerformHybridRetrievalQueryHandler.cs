using MediatR;
using Normora.Shared.Contracts.Documents;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Application.Documents.Queries.PerformHybridRetrieval;

/// <summary>
/// Cross-module query handler executing hybrid retrieval (BM25 full-text + dense vector embeddings)
/// on behalf of the Conversations module without leaking internal DocumentsDbContext dependencies.
/// </summary>
/// <remarks>
/// Modular Monolith Boundary Invariants:
/// - Invoked by <c>ConversationTurnPreparer</c> and <c>SearchCompanyDocumentsTool</c> via MediatR.
/// - Encapsulates all pgvector cosine distance operations and PostgreSQL GIN full-text ranking.
/// - Fuses ranked lists using Reciprocal Rank Fusion (RRF: k=60) inside <see cref="IRetrievalService"/>.
/// - Respects caller's effective department permissions and personal workspace isolation.
/// </remarks>
public sealed class PerformHybridRetrievalQueryHandler(IRetrievalService retrievalService)
    : IRequestHandler<PerformHybridRetrievalQuery, HybridRetrievalResult>
{
    public async Task<HybridRetrievalResult> Handle(PerformHybridRetrievalQuery request, CancellationToken cancellationToken)
    {
        // Delegates to internal domain retrieval service, keeping SQL and vector operators strictly inside Documents.
        var (candidates, vectorSimOfFirst) = await retrievalService.RunHybridRetrievalAsync(
            request.Question,
            request.Limit,
            request.PersonalTenantId,
            cancellationToken);

        return new HybridRetrievalResult(candidates, vectorSimOfFirst);
    }
}

