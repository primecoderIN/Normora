using MediatR;

namespace Normora.Modules.Tenants.Application.Invitations;

/// <summary>
/// Command to accept a pending tenant invitation using a secure token.
/// </summary>
/// <param name="Token">The unique invitation token generated when the invitation was created.</param>
public record AcceptInvitationCommand(Guid Token) : IRequest<bool>;
