using MediatR;
using Normora.Modules.Tenants.Application.Subscription.DTOs;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Subscription.Commands;

/// <summary>
/// Command to generate a self-service customer billing portal session or management redirect URL.
/// Handled by <see cref="CreateCustomerPortalSessionCommandHandler"/>.
/// </summary>
/// <remarks>
/// Authorization & Behavior:
/// - Restricted strictly to tenant owners and administrators (BFLA check).
/// - Provides employers direct access to update billing methods, view historical receipts, or cancel subscriptions.
/// - Implements <see cref="IAuditableRequest"/> for compliance tracing.
/// </remarks>
/// <param name="ReturnUrl">Optional URL to return the administrator to upon leaving the billing portal.</param>
public record CreateCustomerPortalSessionCommand(string? ReturnUrl = null) : IRequest<CustomerPortalResultDto>, IAuditableRequest
{
    public string Action => "Subscription.PortalInitiated";
    public string TargetType => "TenantPlan";
    public string? GetTargetId(object? response) => null;
    public string? GetMetadata(object? response) => null;
}

