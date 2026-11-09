using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Normora.Infrastructure.Extensions;

/// <summary>
/// Registers SignalR real-time messaging services with an optional Redis backplane for multi-process pub/sub broadcast.
/// Enables seamless message delivery between background worker hosts (e.g. Normora.Worker) and HTTP BFF hosts (Normora.Api).
/// </summary>
public static class SignalRServiceExtensions
{
    public static IServiceCollection AddNormoraSignalR(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var signalRBuilder = services.AddSignalR();

        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? configuration["REDIS_CONNECTION"];

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            signalRBuilder.AddStackExchangeRedis(redisConnectionString, options =>
            {
                options.Configuration.ChannelPrefix = RedisChannel.Literal("normora:signalr");
            });
        }

        return services;
    }
}

