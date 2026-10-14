namespace Normora.Shared.Models;

/// <summary>
/// Generic pagination container for paginated query responses.
/// </summary>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);
