using MediatR;
using Normora.Shared.Contracts.Documents;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Documents.Application.Documents.Queries.PerformHybridRetrieval;

public sealed class PerformHybridRetrievalQueryHandler(IRetrievalService retrievalService)
    : IRequestHandler<PerformHybridRetrievalQuery, HybridRetrievalResult>
{
    public async Task<HybridRetrievalResult> Handle(PerformHybridRetrievalQuery request, CancellationToken cancellationToken)
    {
        var (candidates, vectorSimOfFirst) = await retrievalService.RunHybridRetrievalAsync(
            request.Question,
            request.Limit,
            request.PersonalTenantId,
            cancellationToken);

        return new HybridRetrievalResult(candidates, vectorSimOfFirst);
    }
}

