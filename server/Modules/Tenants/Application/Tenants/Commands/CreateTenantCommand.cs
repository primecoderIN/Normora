using MediatR;
using Normora.Modules.Tenants.Application.Tenants.DTOs;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Application.Tenants.Commands;

/// <summary>
/// Command to create a new tenant workspace (company) within the system.
/// </summary>
/// <param name="Name">The display name of the tenant organization.</param>
/// <param name="Slug">The unique URL-safe subdomain identifier for the tenant.</param>
public record CreateTenantCommand(string Name, string Slug) : IRequest<TenantDto>;
