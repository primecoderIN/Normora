using MediatR;
using Normora.Modules.Tenants.Application.Departments.DTOs;

namespace Normora.Modules.Tenants.Application.Departments.Queries;

/// <summary>
/// Query to retrieve a list of all departments for the current tenant.
/// </summary>
public record GetDepartmentsQuery : IRequest<List<DepartmentDto>>;

