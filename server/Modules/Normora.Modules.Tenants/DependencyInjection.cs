using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Normora.Modules.Tenants.Persistence;

namespace Normora.Modules.Tenants;

public static class DependencyInjection
{
    public static IServiceCollection AddTenantsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Host=localhost;Database=normoradb;Username=postgres;Password=password";

        services.AddDbContext<TenantsDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}
