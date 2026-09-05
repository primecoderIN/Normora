using Microsoft.Extensions.DependencyInjection;
using Normora.Api.Middleware;
using System.Text.Json.Serialization;

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
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddHttpContextAccessor();

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
