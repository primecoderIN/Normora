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
        // Pipeline Step 1: Execute Downstream Handler
        // The business mutation executes first. If it throws an exception (e.g. validation error or BOLA),
        // the audit pipeline short-circuits and nothing is recorded in the audit trail.
        var response = await next();

        // Pipeline Step 2: Audit Interception for IAuditableRequest
        // Inspects whether the command implements IAuditableRequest.
        // If so, extracts the mutation action name, affected resource type, target GUID, and serialized JSON delta.
        if (request is IAuditableRequest auditable)
        {
            try
            {
                var tenantId = tenantContext.TenantId;
                if (tenantId.HasValue)
                {
                    var targetId = auditable.GetTargetId(response);
                    var metadata = auditable.GetMetadata(response);

                    // Centralized Audit Record Persistence:
                    // Records actor details (ExternalUserId, Email, DisplayName) alongside timestamp and metadata.
                    await auditService.RecordAsync(
                        tenantId.Value,
                        currentUser.ExternalUserId,
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
                // Fault Tolerance:
                // An audit logging failure is logged as an error but swallowed so it does not fail the primary business transaction.
                logger.LogError(ex, "Failed to record audit log for action {Action}", auditable.Action);
            }
        }

        return response;
    }
}
