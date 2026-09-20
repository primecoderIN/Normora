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

using Normora.Shared.Constants;
using Normora.Shared.Options;

namespace Normora.Api.Extensions;

/// <summary>
/// Rewrites localhost to keycloak for OIDC backchannel requests in Docker
/// </summary>
public class DockerOidcBackchannelHandler : DelegatingHandler
{
    private readonly string _externalHost;
    private readonly string _internalHost;

    public DockerOidcBackchannelHandler(HttpMessageHandler innerHandler, string externalHost = "localhost", string internalHost = "keycloak") : base(innerHandler) 
    { 
        _externalHost = externalHost;
        _internalHost = internalHost;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri != null && request.RequestUri.Host == _externalHost)
        {
            var builder = new UriBuilder(request.RequestUri)
            {
                Host = _internalHost
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
        var keycloakOptions = configuration.GetSection(KeycloakOptions.SectionName).Get<KeycloakOptions>() ?? new KeycloakOptions();

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
                options.Cookie.Name = environment.IsDevelopment() ? AuthConstants.DevCookieName : AuthConstants.ProdCookieName;
                options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
                // Secure should be true in production, false for local development without HTTPS
                options.Cookie.SecurePolicy = environment.IsDevelopment() ? Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest : Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
            })
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.Authority = keycloakOptions.Authority;
                options.MetadataAddress = keycloakOptions.MetadataAddress;
                options.RequireHttpsMetadata = false;

                // Extract hosts from options for Docker backchannel rewriting
                var externalHost = new Uri(keycloakOptions.ExternalAuthority).Host;
                var internalHost = new Uri(keycloakOptions.InternalAuthority).Host;

                // Rewrite external host to internal host for Docker backchannel requests
                options.BackchannelHttpHandler = new DockerOidcBackchannelHandler(new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                }, externalHost, internalHost);

                options.ClientId = AuthConstants.WebClientId;
                // normora-web is a public client in our realm, so no secret is needed, but we MUST use PKCE
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.UsePkce = true;

                // Configure Scopes
                options.Scope.Clear();
                options.Scope.Add(AuthConstants.OpenIdScope);
                options.Scope.Add(AuthConstants.ProfileScope);
                options.Scope.Add(AuthConstants.EmailScope);
                
                // Save tokens into the cookie so the BFF can use them to call downstream APIs (if any)
                options.SaveTokens = true;
                
                options.GetClaimsFromUserInfoEndpoint = true;
                
                // Name and Role claim mappings
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    NameClaimType = AuthConstants.PreferredUsernameClaim,
                    RoleClaimType = AuthConstants.RoleClaim,
                    ValidateIssuer = true,
                    ValidIssuers = new[] 
                    { 
                        keycloakOptions.Authority, 
                        $"{keycloakOptions.ExternalAuthority}/realms/normora" 
                    }
                };

                options.Events = new OpenIdConnectEvents
                {
                    OnRedirectToIdentityProvider = context =>
                    {
                        // The Authority might be internal so the API can reach Keycloak internally,
                        // but we must rewrite the authorize URL to external for the user's browser.
                        context.ProtocolMessage.IssuerAddress = context.ProtocolMessage.IssuerAddress
                            .Replace(keycloakOptions.InternalAuthority, keycloakOptions.ExternalAuthority);

                        // Pass IDP hints (like google, github) to Keycloak if requested
                        if (context.Properties.Items.TryGetValue(AuthConstants.ProviderProperty, out var provider))
                        {
                            context.ProtocolMessage.SetParameter(AuthConstants.KeycloakIdpHintParameter, provider);
                        }
                        return Task.CompletedTask;
                    },
                    OnRedirectToIdentityProviderForSignOut = context =>
                    {
                        // Rewrite the logout URL to external for the user's browser
                        context.ProtocolMessage.IssuerAddress = context.ProtocolMessage.IssuerAddress
                            .Replace(keycloakOptions.InternalAuthority, keycloakOptions.ExternalAuthority);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }
}
