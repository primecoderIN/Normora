using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Normora.Modules.Tenants.Application.Audit;
using Normora.Modules.Tenants.Application.Subscription;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants;

/// <summary>
/// Composition root for the Tenants module.
/// Encapsulates all database context, storage, resolution, payment, and audit service registrations.
/// </summary>
public static class TenantsModuleExtensions
{
    public static IServiceCollection AddTenantsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<TenantsDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IBrandingStorageService, MinioBrandingStorageService>();
        services.AddScoped<ITenantResolutionService, TenantResolutionService>();
        services.AddScoped<IDatabaseMigrator, TenantsDatabaseMigrator>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IPaymentService, PayUPaymentService>();
        services.AddScoped<IPayUPaymentService>(sp => (IPayUPaymentService)sp.GetRequiredService<IPaymentService>());

        return services;
    }
}
