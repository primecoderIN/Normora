using MediatR;
using Normora.Modules.Tenants.Application.Invitations.DTOs;

namespace Normora.Modules.Tenants.Application.Invitations.Queries;

/// <summary>
/// Query to retrieve and preview the details of a pending tenant invitation by its token.
/// Handled by <see cref="GetInvitationQueryHandler"/>.
/// </summary>
/// <remarks>
/// Public Accessibility:
/// - Unauthenticated endpoint that allows an invited employee to view workspace details before logging in.
/// - Returns null if the token does not exist, has expired, or was already claimed.
/// </remarks>
/// <param name="Token">The cryptographic token passed via invitation link query string.</param>
public record GetInvitationQuery(Guid Token) : IRequest<InvitationDto?>;
