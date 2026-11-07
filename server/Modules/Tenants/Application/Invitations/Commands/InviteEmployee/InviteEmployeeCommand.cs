using MediatR;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Invitations.Commands;

/// <summary>
/// Command to issue an email invitation welcoming a new employee into the ambient tenant organization.
/// Handled by <see cref="InviteEmployeeCommandHandler"/>.
/// </summary>
/// <remarks>
/// Quota & Access Checks:
/// - Enforces that the caller has Employer or Admin privileges within the tenant.
/// - Validates organizational seat limits defined by the active subscription tier (<see cref="Domain.TenantPlan"/>).
/// - Generates a secure random 48-hour expiration token.
/// - Queues a transactional email via <see cref="IEmailService"/> using <see cref="TenantEmailTemplates.InvitationEmail"/>.
/// - Implements <see cref="IAuditableRequest"/> to trace user onboarding events.
/// </remarks>
/// <param name="Email">The corporate email address of the prospective employee.</param>
public record InviteEmployeeCommand(string Email) : IRequest<Guid>, IAuditableRequest
{
    public string Action => "Invitation.Sent";
    public string TargetType => "Invitation";
    public string? GetTargetId(object? response) => response is Guid id ? id.ToString() : null;
    public string? GetMetadata(object? response) => System.Text.Json.JsonSerializer.Serialize(new { email = Email });
}

