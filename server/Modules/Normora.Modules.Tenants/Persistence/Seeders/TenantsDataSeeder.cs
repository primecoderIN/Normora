using System.Threading.Tasks;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Persistence.Seeders;

/// <summary>
/// Responsible for populating the Tenants database with initial required records upon application startup.
/// Currently, there is no default data required for this module, but it implements IDataSeeder for future use.
/// </summary>
public class TenantsDataSeeder(TenantsDbContext dbContext) : IDataSeeder
{
    public async Task SeedAsync()
    {
        // Add default seed data here if needed (e.g., a "Default" tenant)
        await Task.CompletedTask;
    }
}
