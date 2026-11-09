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

        // Module-Private DbContext:
        // Registers TenantsDbContext strictly for the Tenants module.
        // Other modules (Documents, Conversations) and API controllers must never inject this DbContext directly.
        services.AddDbContext<TenantsDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Multi-Tenant Ambient Context Resolution:
        // Implements tenant resolution via subdomain, path slug, or X-Tenant-Id header with Redis caching.
        services.AddScoped<ITenantResolutionService, TenantResolutionService>();

        // Schema Isolation & Automatic Database Migrations:
        // Implements IDatabaseMigrator to apply isolated 'tenants' schema migrations during the one-off migration process.
        services.AddScoped<IDatabaseMigrator, TenantsDatabaseMigrator>();

        // Centralized Audit Logging Service:
        // Persists administrative audit trail entries into the tenant's isolated audit tables.
        services.AddScoped<IAuditService, AuditService>();

        // Payment Gateway Integration:
        // Registers PayU India hosted checkout integration for subscription plans, renewals, and webhook processing.
        services.AddScoped<IPaymentService, PayUPaymentService>();

        return services;
    }
}
