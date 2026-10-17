using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Normora.Shared.Interfaces;

namespace Normora.Api.Services.Caching;

/// <summary>
/// Resilient implementation of <see cref="ICacheService"/> utilizing Microsoft's <see cref="IDistributedCache"/>.
/// Automatically handles JSON serialization and safely catches cache failures to prevent downstream request crashes.
/// </summary>
public class DistributedCacheService(
    IDistributedCache cache,
    ILogger<DistributedCacheService> logger) : ICacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await cache.GetStringAsync(key, cancellationToken);
            if (string.IsNullOrEmpty(data))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(data, SerializerOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache read failure for key '{CacheKey}'. Falling back to default.", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, SerializerOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(10)
            };

            await cache.SetStringAsync(key, json, options, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache write failure for key '{CacheKey}'.", key);
        }
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached is not null && !EqualityComparer<T>.Default.Equals(cached, default))
        {
            return cached;
        }

        var result = await factory(cancellationToken);
        if (result is not null)
        {
            await SetAsync(key, result, expiration, cancellationToken);
        }

        return result;
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await cache.RemoveAsync(key, cancellationToken);
            logger.LogDebug("Evicted cache key '{CacheKey}'.", key);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache eviction failure for key '{CacheKey}'.", key);
        }
    }
}
