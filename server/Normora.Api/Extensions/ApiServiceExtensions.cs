using Microsoft.Extensions.DependencyInjection;
using Normora.Api.Middleware;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.RateLimiting;

namespace Normora.Api.Extensions;

/// <summary>
/// Registers web API-specific services, such as Controllers, CORS, OpenAPI, and Exception Handling.
/// </summary>
public static class ApiServiceExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        services.AddControllers()
            .AddJsonOptions(options => 
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        services.AddOpenApi();
        services.AddSignalR();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // Override ASP.NET's default behavior of returning ProblemDetails for bad requests (like malformed JSON)
        services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

                var response = Normora.Shared.ApiResponse<System.Collections.Generic.Dictionary<string, string[]>>.Failure("One or more validation errors occurred.", errors);
                return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(response);
            };
        });

        services.AddHttpContextAccessor();

        // SEC-2 & SEC-3: Rate Limiting
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // "ai" limiter: 20 requests per minute, per user
            options.AddPolicy("ai", context =>
            {
                var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
                return RateLimitPartition.GetFixedWindowLimiter(userId, _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });

            // "anon" limiter: 5 requests per minute, per IP
            options.AddPolicy("anon", context =>
            {
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });
        });

        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy",
                builder => builder
                    .SetIsOriginAllowed(origin =>
                    {
                        // Allow any subdomain of localhost (e.g. intel.localhost:4200)
                        // and the base localhost origins from configuration.
                        var uri = new Uri(origin);
                        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
                        return allowedOrigins.Contains(origin) ||
                               (uri.Host.EndsWith(".localhost") && (uri.Port == 4200 || uri.Port == 80));
                    })
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
        });

        return services;
    }
}
