using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Normora.Api.Extensions;

/// <summary>
/// Registers the JWT authentication and authorization middleware.
/// Validates tokens issued by Keycloak.
/// </summary>
public static class IdentityServiceExtensions
{
    public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = configuration["Keycloak:Authority"];
                options.MetadataAddress = configuration["Keycloak:MetadataAddress"]!;
                options.RequireHttpsMetadata = false;
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // WebSockets cannot reliably send the bearer header during the upgrade,
                        // so accept access_token only for the SignalR hub path, never general APIs.
                        var accessToken = context.Request.Query["access_token"];
                        var requestPath = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) &&
                            requestPath.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    // Docker and browser traffic use different hostnames for the same Keycloak realm.
                    ValidIssuers = new[] 
                    { 
                        configuration["Keycloak:Authority"]!, 
                        "http://localhost:8080/realms/normora" // Handle Docker network issuer mismatch
                    },
                    // The realm export does not consistently emit the web client in `aud`; issuer,
                    // signature, lifetime, and authenticated membership remain enforced.
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true
                };
            });

        services.AddAuthorization();

        return services;
    }
}
