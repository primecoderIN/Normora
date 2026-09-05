using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;

namespace Normora.Modules.Tenants.Application.Invitations;

public class GetInvitationQueryHandler(TenantsDbContext context) : IRequestHandler<GetInvitationQuery, InvitationDto?>
{
    public async Task<InvitationDto?> Handle(GetInvitationQuery request, CancellationToken cancellationToken)
    {
        var invitation = await context.TenantInvitations
            .Include(i => i.Tenant)
            .FirstOrDefaultAsync(i => i.Token == request.Token, cancellationToken);

        if (invitation == null)
        {
            return null;
        }

        return new InvitationDto(
            invitation.Token,
            invitation.Email,
            invitation.Tenant.Name,
            invitation.Tenant.Slug,
            invitation.Status.ToString(),
            invitation.ExpiresAt
        );
    }
}
