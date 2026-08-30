using MediatR;

namespace Normora.Modules.Tenants.Application.Invitations;

public record InviteEmployeeCommand(string Email) : IRequest<Guid>;
