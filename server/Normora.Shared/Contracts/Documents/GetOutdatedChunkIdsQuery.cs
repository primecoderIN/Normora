using MediatR;

namespace Normora.Shared.Contracts.Documents;

/// <summary>
/// Cross-module query to verify if any provided chunk IDs belong to an inactive (outdated) document version.
/// </summary>
public record GetOutdatedChunkIdsQuery(IReadOnlyList<Guid> ChunkIds) : IRequest<HashSet<Guid>>;
