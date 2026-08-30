using FluentValidation;
using MediatR;
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
            typeof(Normora.Modules.Tenants.Application.CreateTenant.CreateTenantCommand).Assembly,
            // Register other assemblies as they implement application logic
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

        return services;
    }
}
