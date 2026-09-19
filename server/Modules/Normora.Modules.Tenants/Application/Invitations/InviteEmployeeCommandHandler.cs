using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Domain;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Invitations;

/// <summary>
/// Handles <see cref="InviteEmployeeCommand"/> by creating an invitation record and sending the invitation email.
/// </summary>
public class InviteEmployeeCommandHandler(
    TenantsDbContext context, 
    ITenantContext tenantContext, 
    IEmailService emailService,
    INotificationService notificationService,
    Microsoft.Extensions.Configuration.IConfiguration configuration) 
    : IRequestHandler<InviteEmployeeCommand, Guid>
{
    public async Task<Guid> Handle(InviteEmployeeCommand request, CancellationToken cancellationToken)
    {
        // Ensure the inviter is acting within the context of a specific tenant before they can invite anyone
        if (!tenantContext.IsTenantResolved || !tenantContext.TenantId.HasValue)
        {
            throw new UnauthorizedAccessException("Tenant context is missing.");
        }

        var tenantId = tenantContext.TenantId.Value;

        // Prevent duplicate invitations by checking if the user already has a pending invite for this specific organization
        var existingInvite = await context.TenantInvitations
            .FirstOrDefaultAsync(i => i.Email == request.Email && i.TenantId == tenantId && i.Status == InvitationStatus.Pending, cancellationToken);

        if (existingInvite != null)
        {
            throw new InvalidOperationException("A pending invitation already exists for this email.");
        }

        // Create the new invitation token which the employee will use to accept the invite
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

        // Dispatch the invitation email containing the secure acceptance link
        var baseUrl = configuration["App:BaseUrl"] ?? "http://localhost:4200";
        var acceptLink = $"{baseUrl}/accept-invite?token={invitation.Token}";
        var body = $"<p>You have been invited to join {tenantName} on Normora.</p><p><a href='{acceptLink}'>Click here to accept the invitation</a>.</p>";
        await emailService.SendEmailAsync(request.Email, $"Invitation to join {tenantName}", body, cancellationToken);

        // Push a real-time notification to the user if they happen to already be logged in to the platform
        await notificationService.NotifyInvitationReceivedAsync(request.Email, tenantName, cancellationToken);

        return invitation.Token;
    }
}
