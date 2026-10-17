namespace Normora.Shared.Interfaces;

/// <summary>
/// Cache abstraction supporting both distributed (Redis) and in-memory caching.
/// Provides safe read, write, atomic get-or-create, and invalidation operations.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Retrieves a cached value by key, or returns default if not found.
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a value in the cache with an optional expiration lifetime.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a cached value, or generates and stores it if missing (Cache-Aside pattern).
    /// </summary>
    Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evicts an item from the cache.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
