using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that retrieves the workspace's white-label branding, colors, and asset logos.
/// </summary>
public sealed class WorkspaceGetBrandingTool(TenantsDbContext dbContext) : IAgentTool
{
    public string Name => "workspace_get_branding";

    public string Description =>
        "Retrieves the active workspace's white-label branding configurations, theme colors, and logo assets.";

    public ToolCategory Category => ToolCategory.ReadOnly;

    public bool RequiresApproval => false;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Checking workspace branding theme...",
        CompletedLabel: "Workspace branding retrieved",
        FailedLabel: "Failed to retrieve branding");

    public object? ParameterSchema => null;

    public async Task<AgentToolResult> ExecuteAsync(
        string argumentsJson,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tenant = await dbContext.Tenants
                .Include(t => t.Branding)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == context.TenantId, cancellationToken);

            if (tenant == null)
            {
                return new AgentToolResult(false, string.Empty, "Workspace tenant not found.");
            }

            var result = new
            {
                workspaceName = tenant.Name,
                slug = tenant.Slug,
                primaryColor = tenant.Branding?.PrimaryColor ?? "#ec4899",
                secondaryColor = tenant.Branding?.SecondaryColor ?? "#6366f1",
                logoUrl = tenant.Branding?.LogoUrl,
                logoUrlDark = tenant.Branding?.LogoUrlDark,
                faviconUrl = tenant.Branding?.FaviconUrl,
                route = "/employer/settings/branding"
            };

            var outputJson = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            return new AgentToolResult(true, outputJson, StructuredData: result);
        }
        catch (Exception ex)
        {
            return new AgentToolResult(false, string.Empty, ex.Message ?? ApiMessages.InternalServerError);
        }
    }
}
