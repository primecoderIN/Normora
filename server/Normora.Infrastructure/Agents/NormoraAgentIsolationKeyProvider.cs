using Microsoft.Agents.AI.Hosting;
using Normora.Shared.Interfaces;

namespace Normora.Infrastructure.Agents;

/// <summary>
/// Provides caller-scoped session isolation for hosted Microsoft Agent Framework agents,
/// partitioning AG-UI threads by both active TenantId and authenticated ExternalUserId.
/// </summary>
public sealed class NormoraAgentIsolationKeyProvider(
    ICurrentUser currentUser,
    ITenantContext tenantContext) : AgentIsolationKeyProvider
{
    public override ValueTask<string?> GetIsolationKeyAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.TenantId?.ToString() ?? "no-tenant";
        var userId = currentUser.ExternalUserId ?? "anonymous";
        return ValueTask.FromResult<string?>($"{tenantId}:{userId}");
    }
}
