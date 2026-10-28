using FluentValidation;
using Hangfire;
using MediatR;
using Normora.Api.Features.Documents;
using Microsoft.Extensions.DependencyInjection;
using Normora.Shared.Interfaces;
using Normora.Api.Services;
using Normora.Api.Services.Email;
using Normora.Shared;
using Normora.Shared.Validation;
using Normora.Modules.Conversations.Application.Exports;
using System.Reflection;

namespace Normora.Api.Extensions;

/// <summary>
/// Registers application-layer services, including MediatR CQRS pipelines, FluentValidation validators,
/// and core cross-cutting shared abstractions (CurrentUser, Email, TenantContext).
/// </summary>
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assemblies = new[]
        {
            typeof(IApiMarker).Assembly, // Register Normora.Api assemblies (Documents module commands)
            typeof(Normora.Modules.Tenants.ITenantsModuleMarker).Assembly, // Register Tenants module commands
            typeof(Normora.Modules.Auth.IAuthModuleMarker).Assembly, // Register Auth module commands
            typeof(Normora.Modules.Users.IUsersModuleMarker).Assembly, // Register Users module commands
            typeof(Normora.Modules.Documents.IDocumentsModuleMarker).Assembly, // Register Documents module commands
            typeof(Normora.Modules.Conversations.IConversationsModuleMarker).Assembly // Register Conversations module commands
        };

        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssemblies(assemblies);
            // Injects the ValidationBehavior into the MediatR pipeline to automatically validate commands.
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            // Injects the AuditBehavior into the MediatR pipeline to record auditable actions.
            cfg.AddOpenBehavior(typeof(Normora.Shared.Audit.AuditBehavior<,>));
        });
        
        services.AddValidatorsFromAssemblies(assemblies);

        // Core Abstractions
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IAuditService, Normora.Modules.Tenants.Application.Audit.AuditService>();
        services.AddScoped<Normora.Modules.Tenants.Application.Subscription.IPayUPaymentService, Normora.Modules.Tenants.Persistence.PayUPaymentService>();
        // Email Services (Resend + Hangfire Background Delivery)
        services.AddHttpClient<ResendEmailSender>();
        services.AddTransient<ResendEmailSender>();
        services.AddTransient<NullEmailSender>();

        services.AddTransient<IEmailSender>(sp =>
        {
            var emailOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Normora.Shared.Options.EmailOptions>>().Value;
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ResendEmailSender>>();

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

        services.AddScoped<Normora.Api.Features.Email.SendEmailJob>();
        services.AddScoped<IEmailService, BackgroundEmailService>();
        services.AddScoped<INotificationService, NotificationService>();

        services.AddScoped<DocumentProcessingJob>();

        services.AddScoped<Normora.Modules.Conversations.Application.Services.IContextResolver, Normora.Modules.Conversations.Application.Services.ContextResolver>();
        services.AddSingleton<Normora.Modules.Conversations.Application.Services.ITokenBudgetService, Normora.Modules.Conversations.Application.Services.TokenBudgetService>();

        services.AddScoped<Normora.Api.Features.Ask.IRetrievalService, Normora.Api.Features.Ask.RetrievalService>();
        services.AddSingleton<Normora.Api.Features.Ask.IAutoTitleService, Normora.Api.Features.Ask.AutoTitleService>();
        services.AddSingleton<Normora.Api.Features.Ask.ConversationMetrics>();

        // Answer exporters — stateless singletons registered against IAnswerExporter.
        // ExportSavedAnswerQueryHandler resolves the correct implementation via IEnumerable<IAnswerExporter>.
        // QuestPDF community licence is set in PdfAnswerExporter's static constructor.
        services.AddSingleton<IAnswerExporter, MarkdownAnswerExporter>();
        services.AddSingleton<IAnswerExporter, PdfAnswerExporter>();
        services.AddSingleton<IAnswerExporter, DocxAnswerExporter>();

        // Agent Tools & Tool Registry (Gemini Tool Calling + MCP)
        services.AddScoped<Normora.Api.Features.Ask.Tools.IAgentTool, Normora.Api.Features.Ask.Tools.CalculatePolicyLimitTool>();
        services.AddScoped<Normora.Api.Features.Ask.Tools.IAgentTool, Normora.Api.Features.Ask.Tools.EscalateInquiryTool>();
        services.AddScoped<Normora.Api.Features.Ask.Tools.IAgentTool, Normora.Api.Features.Ask.Tools.SearchCompanyDocumentsTool>();
        services.AddScoped<Normora.Api.Features.Ask.Tools.IAgentToolRegistry, Normora.Api.Features.Ask.Tools.AgentToolRegistry>();

        return services;
    }
}
