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

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuers = new[] 
                    { 
                        configuration["Keycloak:Authority"]!, 
                        "http://localhost:8080/realms/normora" // Handle Docker network issuer mismatch
                    },
                    ValidateAudience = false, // Keycloak doesn't always add the clientId to 'aud' by default
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true
                };
            });

        services.AddAuthorization();

        return services;
    }
}
