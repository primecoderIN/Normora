using MediatR;

namespace Normora.Shared.Contracts.Documents;

/// <summary>
/// Cross-module query to fetch file names of ready documents accessible company-wide or in the given effective departments.
/// </summary>
public record GetAccessibleDocumentFileNamesQuery(
    IReadOnlyList<Guid> EffectiveDepartmentIds,
    int Limit = 20) : IRequest<IReadOnlyList<string>>;
