using MediatR;
using Microsoft.Extensions.Logging;
using Normora.Shared.Interfaces;

namespace Normora.Shared.Audit;

/// <summary>
/// A MediatR pipeline behavior that automatically intercepts and records audit entries
/// when an auditable command completes successfully.
/// </summary>
public sealed class AuditBehavior<TRequest, TResponse>(
    IAuditService auditService,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    ILogger<AuditBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        if (request is IAuditableRequest auditable)
        {
            try
            {
                var tenantId = tenantContext.TenantId;
                if (tenantId.HasValue)
                {
                    var targetId = auditable.GetTargetId(response);
                    var metadata = auditable.GetMetadata(response);

                    await auditService.RecordAsync(
                        tenantId.Value,
                        currentUser.KeycloakUserId,
                        currentUser.Email,
                        currentUser.DisplayName,
                        auditable.Action,
                        auditable.TargetType,
                        targetId,
                        metadata,
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to record audit log for action {Action}", auditable.Action);
            }
        }

        return response;
    }
}
