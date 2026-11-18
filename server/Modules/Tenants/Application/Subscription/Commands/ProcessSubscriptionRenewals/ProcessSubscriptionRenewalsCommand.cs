using MediatR;

namespace Normora.Modules.Tenants.Application.Subscription.Commands.ProcessSubscriptionRenewals;

/// <summary>
/// Result summary of an automated subscription renewal processing run.
/// </summary>
public record SubscriptionRenewalsResultDto(
    int TotalProcessed,
    int SuccessfulRenewals,
    int FailedRenewals
);

/// <summary>
/// Command dispatched by the scheduled Hangfire background worker to process due subscription renewals.
/// </summary>
public record ProcessSubscriptionRenewalsCommand : IRequest<SubscriptionRenewalsResultDto>;
