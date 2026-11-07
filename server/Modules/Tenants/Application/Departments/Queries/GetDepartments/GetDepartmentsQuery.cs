using MediatR;
using Normora.Modules.Tenants.Application.Departments.DTOs;

namespace Normora.Modules.Tenants.Application.Departments.Queries;

/// <summary>
/// Query to retrieve all active departments configured within the caller's tenant organization.
/// Handled by <see cref="GetDepartmentsQueryHandler"/>.
/// </summary>
/// <remarks>
/// Ambient Tenant Scoping:
/// - Automatically scopes to the current TenantId via ITenantContext.
/// - Returns sorted alphabetically by name to power clean UI selectors across document upload and user management.
/// </remarks>
public record GetDepartmentsQuery : IRequest<List<DepartmentDto>>;

