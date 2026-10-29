using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Contracts.Tenants;

namespace Normora.Modules.Tenants.Application.Users;

/// <summary>
/// Handles GetTenantAdminRecipientsQuery within the Tenants module boundary.
/// Retrieves the organization name and active administrator email addresses.
/// </summary>
public sealed class GetTenantAdminRecipientsQueryHandler(TenantsDbContext dbContext)
    : IRequestHandler<GetTenantAdminRecipientsQuery, TenantAdminRecipientsResult>
{
    public async Task<TenantAdminRecipientsResult> Handle(
        GetTenantAdminRecipientsQuery request,
        CancellationToken cancellationToken)
    {
        var tenant = await dbContext.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        var tenantName = tenant?.Name ?? "Your Organization";

        var adminEmails = await dbContext.TenantMemberships
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(m => m.TenantId == request.TenantId && m.Role == TenantRole.Admin && m.RemovedAt == null)
            .Include(m => m.User)
            .Where(m => !string.IsNullOrEmpty(m.User.Email))
            .Select(m => m.User.Email!)
            .Distinct()
            .ToListAsync(cancellationToken);

        return new TenantAdminRecipientsResult(tenantName, adminEmails);
    }
}
