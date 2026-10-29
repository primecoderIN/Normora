using Microsoft.EntityFrameworkCore;
using Normora.Modules.Documents.Domain;
using Normora.Modules.Documents.Persistence;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;
using Xunit;

namespace Normora.Architecture.Tests;

public sealed class TenantResolutionServiceTests
{
    [Fact]
    public async Task ResolveTenantAccessAsync_ReflectsMembershipRevocationImmediately()
    {
        var options = new DbContextOptionsBuilder<TenantsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new TenantsDbContext(options);

        var tenantId = Guid.NewGuid();
        var user = new User
        {
            Id = Guid.NewGuid(),
            ExternalUserId = "external-user"
        };
        var membership = new TenantMembership
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = user.Id,
            Role = TenantRole.Employee,
            User = user,
            Tenant = new Tenant
            {
                Id = tenantId,
                Name = "Tenant",
                Slug = "tenant"
            }
        };

        context.TenantMemberships.Add(membership);
        await context.SaveChangesAsync();

        var service = new TenantResolutionService(context);
        Assert.NotNull(await service.ResolveTenantAccessAsync(tenantId, user.ExternalUserId));

        membership.RemovedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        Assert.Null(await service.ResolveTenantAccessAsync(tenantId, user.ExternalUserId));
    }

    [Fact]
    public void PurgeBatchQuery_TranslatesGuidCursorToPostgres()
    {
        var options = new DbContextOptionsBuilder<DocumentsDbContext>()
            .UseNpgsql("Host=localhost;Database=normora;Username=normora;Password=normora", npgsql => npgsql.UseVector())
            .Options;
        using var context = new DocumentsDbContext(options, new UnresolvedTenantContext());
        Guid? lastDocumentId = Guid.NewGuid();
        var cutoffDate = DateTime.UtcNow;

        var sql = context.Documents
            .IgnoreQueryFilters()
            .Where(document =>
                document.DeletedAt != null &&
                document.DeletedAt < cutoffDate &&
                (!lastDocumentId.HasValue || document.Id.CompareTo(lastDocumentId.Value) > 0))
            .OrderBy(document => document.Id)
            .Take(100)
            .ToQueryString();

        Assert.Contains(">", sql);
    }

    private sealed class UnresolvedTenantContext : ITenantContext
    {
        public Guid? TenantId => null;
        public string? TenantRole => null;
        public IReadOnlyCollection<Guid> EffectiveDepartments => Array.Empty<Guid>();
        public bool IsTenantResolved => false;

        public void SetContext(Guid tenantId, string role, IReadOnlyCollection<Guid> effectiveDepartments)
        {
            throw new NotSupportedException();
        }
    }
}
