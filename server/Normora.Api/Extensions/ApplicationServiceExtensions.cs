using FluentValidation;
using Hangfire;
using MediatR;
using Normora.Api.Features.Documents;
using Microsoft.Extensions.DependencyInjection;
using Normora.Shared.Interfaces;
using Normora.Api.Services;
using Normora.Shared;
using Normora.Shared.Validation;
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
            typeof(Normora.Modules.Documents.IDocumentsModuleMarker).Assembly // Register Documents module commands
        };

        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssemblies(assemblies);
            // Injects the ValidationBehavior into the MediatR pipeline to automatically validate commands.
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        
        services.AddValidatorsFromAssemblies(assemblies);

        // Core Abstractions
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IEmailService, SmtpEmailService>();

        services.AddScoped<DocumentProcessingJob>();

        return services;
    }
}
