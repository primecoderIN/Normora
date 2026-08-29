using MediatR;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Application.CreateTenant;

public record CreateTenantCommand(string Name, string Slug) : IRequest<Tenant>;
