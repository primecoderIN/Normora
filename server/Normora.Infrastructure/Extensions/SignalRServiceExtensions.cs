using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Normora.Infrastructure.Options;
using StackExchange.Redis;

namespace Normora.Infrastructure.Extensions;

/// <summary>
/// Registers SignalR real-time messaging services with an optional Redis backplane for multi-process pub/sub broadcast.
/// Enables seamless message delivery between background worker hosts (e.g. Normora.Worker) and HTTP BFF hosts (Normora.Api).
/// </summary>
public static class SignalRServiceExtensions
{
    /// <summary>
    /// Registers SignalR real-time messaging services using strongly typed <see cref="IOptions{RedisOptions}"/>.
    /// When a Redis connection string is provided, enables the Redis pub/sub backplane for cross-process event broadcasting.
    /// </summary>
    public static IServiceCollection AddNormoraSignalR(
        this IServiceCollection services,
        IOptions<RedisOptions> redisOptions)
    {
        var signalRBuilder = services.AddSignalR();
        var redis = redisOptions.Value;

        if (!string.IsNullOrWhiteSpace(redis.ConnectionString))
        {
            signalRBuilder.AddStackExchangeRedis(redis.ConnectionString, options =>
            {
                options.Configuration.ChannelPrefix = RedisChannel.Literal(redis.ChannelPrefix);
            });
        }

        return services;
    }

    /// <summary>
    /// Registers SignalR real-time messaging services by binding <see cref="RedisOptions"/> from configuration
    /// and delegating to the <see cref="IOptions{RedisOptions}"/> registration overload.
    /// </summary>
    public static IServiceCollection AddNormoraSignalR(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisOptions = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>() ?? new RedisOptions();
        if (string.IsNullOrWhiteSpace(redisOptions.ConnectionString))
        {
            redisOptions.ConnectionString = configuration.GetConnectionString(RedisOptions.SectionName)
                ?? configuration[RedisOptions.EnvironmentVariable];
        }

        return services.AddNormoraSignalR(Microsoft.Extensions.Options.Options.Create(redisOptions));
    }
}

