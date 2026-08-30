using MediatR;
using Normora.Modules.Tenants.Domain;

namespace Normora.Modules.Tenants.Application.CreateTenant;

public record TenantDto(Guid Id, string Name, string Slug, int Status, DateTime CreatedAt);

public record CreateTenantCommand(string Name, string Slug) : IRequest<TenantDto>;
