using MediatR;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Invitations;

/// <summary>
/// Command to send an email invitation to a new employee to join the current tenant.
/// </summary>
/// <param name="Email">The email address of the employee to invite.</param>
public record InviteEmployeeCommand(string Email) : IRequest<Guid>, IAuditableRequest
{
    public string Action => "Invitation.Sent";
    public string TargetType => "Invitation";
    public string? GetTargetId(object? response) => response is Guid id ? id.ToString() : null;
    public string? GetMetadata(object? response) => System.Text.Json.JsonSerializer.Serialize(new { email = Email });
}

