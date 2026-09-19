namespace Normora.Shared.Interfaces;

public interface INotificationService
{
    Task NotifyInvitationReceivedAsync(string email, string tenantName, CancellationToken cancellationToken = default);
}
