using MediatR;

namespace Normora.Modules.Tenants.Application.Invitations;

public record RejectInvitationCommand(Guid Token) : IRequest<bool>;
