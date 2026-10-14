using MediatR;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Invitations;

/// <summary>
/// Command to accept a pending tenant invitation using a secure token.
/// </summary>
/// <param name="Token">The unique invitation token generated when the invitation was created.</param>
public record AcceptInvitationCommand(Guid Token) : IRequest<bool>, IAuditableRequest
{
    public string Action => "Invitation.Accepted";
    public string TargetType => "Invitation";
    public string? GetTargetId(object? response) => Token.ToString();
    public string? GetMetadata(object? response) => null;
}

