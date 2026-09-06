using MediatR;

namespace Normora.Modules.Tenants.Application.Invitations;

/// <summary>
/// Command to send an email invitation to a new employee to join the current tenant.
/// </summary>
/// <param name="Email">The email address of the employee to invite.</param>
public record InviteEmployeeCommand(string Email) : IRequest<Guid>;
