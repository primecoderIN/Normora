using MediatR;
using Normora.Modules.Tenants.Application.Branding.DTOs;

namespace Normora.Modules.Tenants.Application.Branding.Queries;

/// <summary>
/// Query to retrieve a tenant's public white-label branding theme using their vanity URL slug.
/// Handled by <see cref="GetTenantBrandingQueryHandler"/>.
/// </summary>
/// <remarks>
/// Public Accessibility:
/// - Unauthenticated endpoint designed for pre-login theme hydration.
/// - Results are cached in IMemoryCache with a sliding expiration to minimize database traffic during login traffic spikes.
/// </remarks>
/// <param name="Slug">The unique URL slug of the tenant (e.g. "acme-corp").</param>
public record GetTenantBrandingQuery(string Slug) : IRequest<TenantBrandingDto?>;

