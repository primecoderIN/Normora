namespace Normora.Shared.Interfaces;

/// <summary>
/// Service interface for dispatching tenant notifications and invitations.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends an email notification to an invited user containing the tenant onboarding link.
    /// </summary>
    Task NotifyInvitationReceivedAsync(string email, string tenantName, CancellationToken cancellationToken = default);
}

