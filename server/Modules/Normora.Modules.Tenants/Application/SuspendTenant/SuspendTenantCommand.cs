using MediatR;

namespace Normora.Modules.Tenants.Application.SuspendTenant;

public record SuspendTenantCommand(Guid TenantId) : IRequest<bool>;
