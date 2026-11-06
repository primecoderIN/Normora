using MediatR;
using Normora.Modules.Tenants.Application.Subscription.DTOs;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Subscription.Commands;

public record CreateCustomerPortalSessionCommand(string? ReturnUrl = null) : IRequest<CustomerPortalResultDto>, IAuditableRequest
{
    public string Action => "Subscription.PortalInitiated";
    public string TargetType => "TenantPlan";
    public string? GetTargetId(object? response) => null;
    public string? GetMetadata(object? response) => null;
}

