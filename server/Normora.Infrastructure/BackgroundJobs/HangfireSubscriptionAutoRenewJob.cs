using Hangfire;
using MediatR;
using Normora.Modules.Tenants.Application.Subscription.Commands.ProcessSubscriptionRenewals;

namespace Normora.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire recurring job that triggers the subscription renewal processor across all tenant workspaces.
/// </summary>
public sealed class HangfireSubscriptionAutoRenewJob(IMediator mediator)
{
    [AutomaticRetry(Attempts = 2)]
    public Task ExecuteAsync(CancellationToken cancellationToken) =>
        mediator.Send(new ProcessSubscriptionRenewalsCommand(), cancellationToken);
}
