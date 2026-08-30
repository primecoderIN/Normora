using MediatR;

namespace Normora.Modules.Tenants.Application.Invitations;

public record AcceptInvitationCommand(Guid Token) : IRequest<bool>;
