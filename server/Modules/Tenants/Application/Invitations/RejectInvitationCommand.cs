using MediatR;

namespace Normora.Modules.Tenants.Application.Invitations;

/// <summary>Rejects a pending invitation after handler-level identity and expiry validation.</summary>
public record RejectInvitationCommand(Guid Token) : IRequest<bool>;
