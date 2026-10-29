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
            // Validation pipeline behavior
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            // Audit pipeline behavior
            cfg.AddOpenBehavior(typeof(AuditBehavior<,>));
        });

        services.AddValidatorsFromAssemblies(assemblies);

        // Core Host Abstractions
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<ITenantContext, TenantContext>();

        // Email Services (Resend + Hangfire Background Delivery)
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
