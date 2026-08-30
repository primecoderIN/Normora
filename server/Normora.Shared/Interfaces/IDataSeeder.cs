using System.Threading.Tasks;

namespace Normora.Shared.Interfaces;

/// <summary>
/// A standardized interface for seeding initial data into a specific module's database.
/// During application startup, modules can register implementations of IDataSeeder,
/// which are then sequentially executed to ensure necessary default data (like admin users or roles) exists.
/// </summary>
public interface IDataSeeder
{
    /// <summary>
    /// Executes the seeding logic. Implementations should ensure this operation is idempotent
    /// (safe to run multiple times without corrupting existing data).
    /// </summary>
    Task SeedAsync();
}
