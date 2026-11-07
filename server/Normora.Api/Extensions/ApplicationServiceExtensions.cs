using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Normora.Api.Features.Email;
using Normora.Api.Services;
using Normora.Api.Services.Email;
using Normora.Shared.Audit;
using Normora.Shared.Interfaces;
using Normora.Shared.Options;
using Normora.Shared.Validation;

namespace Normora.Api.Extensions;

/// <summary>
/// Registers application-layer services, including MediatR CQRS pipelines, FluentValidation validators,
/// and core cross-cutting host abstractions (CurrentUser, Email, TenantContext).
/// </summary>
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Multi-Assembly MediatR & CQRS Assembly Discovery:
        // Registers MediatR handlers, notification subscribers, and stream request handlers across
        // all 3 business modules (Tenants, Documents, Conversations) plus the host BFF (Normora.Api).
        var assemblies = new[]
        {
            typeof(IApiMarker).Assembly,                                            // Normora.Api (Dashboard, Email)
            typeof(Normora.Modules.Tenants.ITenantsModuleMarker).Assembly,          // Tenants module
            typeof(Normora.Modules.Documents.IDocumentsModuleMarker).Assembly,      // Documents module
            typeof(Normora.Modules.Conversations.IConversationsModuleMarker).Assembly // Conversations module
        };

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(assemblies);
            // Open Generic Pipeline Behaviors:
            // 1. ValidationBehavior: Intercepts incoming commands/queries and evaluates all registered FluentValidators.
            // 2. AuditBehavior: Intercepts commands implementing IAuditableRequest and persists audit log entries.
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(AuditBehavior<,>));
        });

        // FluentValidation Auto-Scanning:
        // Registers all AbstractValidator<T> implementations from the modular assemblies.
        services.AddValidatorsFromAssemblies(assemblies);

        // Ambient Host & Security Abstractions:
        // - ICurrentUser: Bridges ClaimsPrincipal to strongly-typed identity claims (ExternalUserId, Email, Roles).
        // - ITenantContext: Scoped ambient tenant container populated by TenantResolutionMiddleware.
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<Normora.Api.Services.DatabaseMigrationRunner>();

        // Email Dispatching Infrastructure:
        // Integrates Resend HTTP API for transactional emails (invitations, alerts) with NullEmailSender dev fallback.
        services.AddHttpClient<ResendEmailSender>();
        services.AddTransient<ResendEmailSender>();
        services.AddTransient<NullEmailSender>();

        services.AddTransient<IEmailSender>(sp =>
        {
            var emailOptions = sp.GetRequiredService<IOptions<EmailOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<ResendEmailSender>>();

            if (string.Equals(emailOptions.Provider, "Null", StringComparison.OrdinalIgnoreCase))
            {
                return sp.GetRequiredService<NullEmailSender>();
            }

            if (!string.IsNullOrWhiteSpace(emailOptions.ApiKey))
            {
                return sp.GetRequiredService<ResendEmailSender>();
            }

            logger.LogWarning("No email API key detected in configuration. Falling back to NullEmailSender (emails will be logged to console instead of dispatched).");
            return sp.GetRequiredService<NullEmailSender>();
        });

        services.AddScoped<SendEmailJob>();
        services.AddScoped<IEmailService, BackgroundEmailService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IDocumentNotificationService, SignalRDocumentNotificationService>();

        return services;
    }
}
