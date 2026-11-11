namespace Normora.Infrastructure.Options;

/// <summary>
/// Strongly-typed configuration options for Redis distributed caching and SignalR pub/sub backplane.
/// Eliminates hardcoded magic strings across infrastructure extensions.
/// </summary>
public class RedisOptions
{
    public const string SectionName = "Redis";
    public const string EnvironmentVariable = "REDIS_CONNECTION";
    public const string DefaultChannelPrefix = "normora:signalr";
    public const string DefaultInstanceName = "normora:";

    /// <summary>
    /// Connection string or host:port endpoint for the Redis server.
    /// Injected via <c>Redis:ConnectionString</c>, <c>ConnectionStrings:Redis</c>, or <c>REDIS_CONNECTION</c>.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Channel prefix for SignalR pub/sub backplane messaging.
    /// Defaults to <c>normora:signalr</c>.
    /// </summary>
    public string ChannelPrefix { get; set; } = DefaultChannelPrefix;

    /// <summary>
    /// Key prefix for distributed cache entries.
    /// Defaults to <c>normora:</c>.
    /// </summary>
    public string InstanceName { get; set; } = DefaultInstanceName;
}
