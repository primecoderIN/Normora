using MediatR;
using Normora.Modules.Tenants.Application.Invitations.DTOs;

namespace Normora.Modules.Tenants.Application.Invitations.Queries;

/// <summary>
/// Query to retrieve the details of a pending tenant invitation by its token.
/// </summary>
/// <param name="Token">The unique token identifying the invitation.</param>
public record GetInvitationQuery(Guid Token) : IRequest<InvitationDto?>;
