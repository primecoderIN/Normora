using MediatR;
using Normora.Shared.Interfaces;

namespace Normora.Shared.Contracts.Documents;

public record HybridRetrievalResult(
    List<RetrievalCandidate> Candidates,
    double VectorSimOfFirst);

/// <summary>
/// Cross-module query to execute hybrid dense/sparse retrieval and rank fusion without leaking DocumentsDbContext or IRetrievalService.
/// </summary>
public record PerformHybridRetrievalQuery(
    string Question,
    int Limit,
    Guid? PersonalTenantId) : IRequest<HybridRetrievalResult>;

