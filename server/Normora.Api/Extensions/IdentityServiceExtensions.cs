using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Duende.Bff;
using Duende.Bff.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Normora.Api.Extensions;

/// <summary>
/// Rewrites localhost to keycloak for OIDC backchannel requests in Docker
/// </summary>
public class DockerOidcBackchannelHandler : DelegatingHandler
{
    public DockerOidcBackchannelHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri != null && request.RequestUri.Host == "localhost")
        {
            var builder = new UriBuilder(request.RequestUri)
            {
                Host = "keycloak"
            };
            request.RequestUri = builder.Uri;
        }
        return base.SendAsync(request, cancellationToken);
    }
}

/// <summary>
/// Registers the BFF (Backend-For-Frontend) Authentication middleware.
/// Validates tokens issued by Keycloak and issues HttpOnly Cookies to the SPA.
/// </summary>
public static class IdentityServiceExtensions
{
    public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        // Prevents mapping standard claim types (like 'sub') to Microsoft proprietary schemas
        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

        services.AddBff()
        .AddEntityFrameworkServerSideSessions(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), sql => sql.MigrationsAssembly("Normora.Api"));
        })
        .AddSessionCleanupBackgroundProcess();

        services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                options.DefaultSignOutScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.Name = environment.IsDevelopment() ? "normora-auth" : "__Host-spa";
                options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
                // Secure should be true in production, false for local development without HTTPS
                options.Cookie.SecurePolicy = environment.IsDevelopment() ? Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest : Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
            })
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.Authority = configuration["Keycloak:Authority"];
                options.MetadataAddress = configuration["Keycloak:MetadataAddress"]!;
                options.RequireHttpsMetadata = false;

                // Rewrite localhost to keycloak for internal Docker backchannel requests
                options.BackchannelHttpHandler = new DockerOidcBackchannelHandler(new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                });

                options.ClientId = "normora-web";
                // normora-web is a public client in our realm, so no secret is needed, but we MUST use PKCE
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.UsePkce = true;

                // Configure Scopes
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                
                // Save tokens into the cookie so the BFF can use them to call downstream APIs (if any)
                options.SaveTokens = true;
                
                options.GetClaimsFromUserInfoEndpoint = true;
                
                // Name and Role claim mappings
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    NameClaimType = "preferred_username",
                    RoleClaimType = "role",
                    ValidateIssuer = true,
                    ValidIssuers = new[] 
                    { 
                        configuration["Keycloak:Authority"]!, 
                        "http://localhost:8080/realms/normora" 
                    }
                };

                options.Events = new OpenIdConnectEvents
                {
                    OnRedirectToIdentityProvider = context =>
                    {
                        // The Authority is http://keycloak:8080 so the API can reach Keycloak internally,
                        // but we must rewrite the authorize URL to localhost for the user's browser.
                        context.ProtocolMessage.IssuerAddress = context.ProtocolMessage.IssuerAddress
                            .Replace("http://keycloak:8080", "http://localhost:8080");

                        // Pass IDP hints (like google, github) to Keycloak if requested
                        if (context.Properties.Items.TryGetValue("provider", out var provider))
                        {
                            context.ProtocolMessage.SetParameter("kc_idp_hint", provider);
                        }
                        return Task.CompletedTask;
                    },
                    OnRedirectToIdentityProviderForSignOut = context =>
                    {
                        // Rewrite the logout URL to localhost for the user's browser
                        context.ProtocolMessage.IssuerAddress = context.ProtocolMessage.IssuerAddress
                            .Replace("http://keycloak:8080", "http://localhost:8080");
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }
}
