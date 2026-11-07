using MediatR;
using Normora.Modules.Tenants.Application.Tenants.DTOs;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Application.Tenants.Commands;

/// <summary>
/// Command to provision a new tenant workspace (company) in Normora.
/// Handled by <see cref="CreateTenantCommandHandler"/>.
/// </summary>
/// <remarks>
/// Lifecycle & Bootstrapping:
/// - Enforces global uniqueness of the tenant slug to guarantee collision-free vanity routing.
/// - Automatically creates the default "General" organizational department.
/// - Grants the initiating authenticated user the primary "Owner" membership role.
/// - Initializes the default Free plan tier (<see cref="Domain.TenantPlan"/>) with baseline quotas.
/// - Bootstraps default white-label branding variables.
/// </remarks>
/// <param name="Name">The display name of the tenant organization (e.g. "Acme Corporation").</param>
/// <param name="Slug">The unique URL-safe subdomain identifier (e.g. "acme-corp").</param>
public record CreateTenantCommand(string Name, string Slug) : IRequest<TenantDto>;
