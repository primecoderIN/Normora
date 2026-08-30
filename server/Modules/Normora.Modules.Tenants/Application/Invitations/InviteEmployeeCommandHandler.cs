using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Invitations;

public class InviteEmployeeCommandHandler(
    TenantsDbContext context, 
    ITenantContext tenantContext, 
    IEmailService emailService) 
    : IRequestHandler<InviteEmployeeCommand, Guid>
{
    public async Task<Guid> Handle(InviteEmployeeCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.IsTenantResolved || !tenantContext.TenantId.HasValue)
        {
            throw new UnauthorizedAccessException("Tenant context is missing.");
        }

        var tenantId = tenantContext.TenantId.Value;

        // Ensure no pending invitation exists for this email and tenant
        var existingInvite = await context.TenantInvitations
            .FirstOrDefaultAsync(i => i.Email == request.Email && i.TenantId == tenantId && i.Status == "Pending", cancellationToken);

        if (existingInvite != null)
        {
            throw new InvalidOperationException("A pending invitation already exists for this email.");
        }

        var invitation = new TenantInvitation
        {
            Email = request.Email,
            TenantId = tenantId
        };

        context.TenantInvitations.Add(invitation);
        await context.SaveChangesAsync(cancellationToken);

        // Retrieve tenant name for the email
        var tenant = await context.Tenants.FindAsync(new object[] { tenantId }, cancellationToken);
        var tenantName = tenant?.Name ?? "An organization";

        // Send email
        var acceptLink = $"http://localhost:4200/accept-invite?token={invitation.Token}";
        var body = $"<p>You have been invited to join {tenantName} on Normora.</p><p><a href='{acceptLink}'>Click here to accept the invitation</a>.</p>";
        await emailService.SendEmailAsync(request.Email, $"Invitation to join {tenantName}", body, cancellationToken);

        return invitation.Token;
    }
}
