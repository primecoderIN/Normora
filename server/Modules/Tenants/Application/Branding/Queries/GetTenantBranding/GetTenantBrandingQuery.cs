using MediatR;
using Normora.Modules.Tenants.Application.Branding.DTOs;

namespace Normora.Modules.Tenants.Application.Branding.Queries;

/// <summary>
/// Query to retrieve a tenant's branding information based on their slug.
/// </summary>
/// <param name="Slug">The unique URL slug of the tenant.</param>
public record GetTenantBrandingQuery(string Slug) : IRequest<TenantBrandingDto?>;

