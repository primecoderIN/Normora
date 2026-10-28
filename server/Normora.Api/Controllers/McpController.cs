using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Normora.Api.Features.Ask.Tools;
using Normora.Api.Middleware;
using Normora.Modules.Documents.Persistence;
using Normora.Modules.Tenants.Persistence;
using Normora.Shared.Constants;
using Normora.Shared.Exceptions;
using Normora.Shared.Interfaces;

namespace Normora.Api.Controllers;

/// <summary>
/// Exposes Normora's agent tools and company knowledge resources via the Model Context Protocol (MCP) JSON-RPC 2.0 specification.
/// Enforces strict tenant isolation and BOLA/BFLA authorization.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[RequireTenant(TenantRoles.Admin, TenantRoles.Employee)]
[Produces("application/json")]
public sealed class McpController(
    IAgentToolRegistry toolRegistry,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    TenantsDbContext tenantsContext,
    DocumentsDbContext documentsDbContext) : ControllerBase
{
    private const string ProtocolVersion = "2024-11-05";

    /// <summary>
    /// Gets discovery manifest and capabilities for this workspace's MCP server endpoint.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetManifest(CancellationToken cancellationToken)
    {
        var tenant = await tenantsContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantContext.TenantId, cancellationToken);

        var manifest = new
        {
            server = "Normora Enterprise MCP Server",
            version = "1.0.0",
            protocolVersion = ProtocolVersion,
            workspace = new
            {
                tenantId = tenantContext.TenantId,
                tenantName = tenant?.Name,
                slug = tenant?.Slug
            },
            toolsCount = toolRegistry.Tools.Count,
            capabilities = new
            {
                tools = new { listChanged = false },
                resources = new { subscribe = false, listChanged = false }
            }
        };

        return Ok(manifest);
    }

    /// <summary>
    /// Primary JSON-RPC 2.0 endpoint for Model Context Protocol interactions.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> HandleJsonRpc(
        [FromBody] McpJsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Method))
        {
            return BadRequest(McpJsonRpcResponse.Failure(null, -32600, ApiMessages.McpInvalidRequest));
        }

        try
        {
            var response = request.Method switch
            {
                "initialize" => HandleInitialize(request),
                "notifications/initialized" => McpJsonRpcResponse.Success(request.Id, new { status = "ready" }),
                "ping" => McpJsonRpcResponse.Success(request.Id, new { }),
                "tools/list" => HandleToolsList(request),
                "tools/call" => await HandleToolsCallAsync(request, cancellationToken),
                "resources/list" => await HandleResourcesListAsync(request, cancellationToken),
                "resources/read" => await HandleResourcesReadAsync(request, cancellationToken),
                _ => McpJsonRpcResponse.Failure(request.Id, -32601, ApiMessages.McpMethodNotFound)
            };

            return Ok(response);
        }
        catch (BolaException)
        {
            return Ok(McpJsonRpcResponse.Failure(request.Id, -32004, ApiMessages.McpResourceNotFound));
        }
        catch (BflaException)
        {
            return StatusCode(StatusCodes.Status403Forbidden, McpJsonRpcResponse.Failure(request.Id, -32003, ApiMessages.Forbidden));
        }
        catch (Exception ex)
        {
            return Ok(McpJsonRpcResponse.Failure(request.Id, -32603, ex.Message));
        }
    }

    private static McpJsonRpcResponse HandleInitialize(McpJsonRpcRequest request)
    {
        var result = new
        {
            protocolVersion = ProtocolVersion,
            capabilities = new
            {
                tools = new { listChanged = false },
                resources = new { subscribe = false, listChanged = false }
            },
            serverInfo = new
            {
                name = "Normora Enterprise MCP Server",
                version = "1.0.0"
            }
        };

        return McpJsonRpcResponse.Success(request.Id, result);
    }

    private McpJsonRpcResponse HandleToolsList(McpJsonRpcRequest request)
    {
        var tools = toolRegistry.GetMcpToolDefinitions();
        return McpJsonRpcResponse.Success(request.Id, new { tools });
    }

    private async Task<McpJsonRpcResponse> HandleToolsCallAsync(
        McpJsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.Params.HasValue)
        {
            return McpJsonRpcResponse.Failure(request.Id, -32602, "Parameters object is required for tools/call.");
        }

        var paramsObj = request.Params.Value;
        if (!paramsObj.TryGetProperty("name", out var nameProp) || string.IsNullOrWhiteSpace(nameProp.GetString()))
        {
            return McpJsonRpcResponse.Failure(request.Id, -32602, "Tool 'name' is required.");
        }

        var toolName = nameProp.GetString()!;
        var tool = toolRegistry.FindTool(toolName);
        if (tool == null)
        {
            return McpJsonRpcResponse.Failure(request.Id, -32601, ApiMessages.McpToolNotFound);
        }

        var argsJson = paramsObj.TryGetProperty("arguments", out var argsProp)
            ? argsProp.GetRawText()
            : "{}";

        // Build authenticated user & tenant context
        var personalTenantId = await tenantsContext.TenantMemberships
            .AsNoTracking()
            .Where(m => m.User.ExternalUserId == currentUser.ExternalUserId && m.Tenant.IsPersonal)
            .Select(m => (Guid?)m.TenantId)
            .FirstOrDefaultAsync(cancellationToken);

        var activeTenant = await tenantsContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantContext.TenantId, cancellationToken);

        var toolContext = new ToolExecutionContext(
            tenantContext.TenantId!.Value,
            activeTenant?.Name ?? "Workspace",
            currentUser.KeycloakUserId,
            currentUser.Email,
            currentUser.DisplayName,
            personalTenantId);

        var toolResult = await toolRegistry.ExecuteToolAsync(toolName, argsJson, toolContext, cancellationToken);

        var contentText = !string.IsNullOrWhiteSpace(toolResult.Output)
            ? toolResult.Output
            : (toolResult.Error ?? string.Empty);

        var result = new
        {
            content = new[]
            {
                new
                {
                    type = "text",
                    text = contentText
                }
            },
            isError = !toolResult.Success
        };

        return McpJsonRpcResponse.Success(request.Id, result);
    }

    private async Task<McpJsonRpcResponse> HandleResourcesListAsync(
        McpJsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        var effectiveDepts = tenantContext.EffectiveDepartments.ToHashSet();
        var isAdmin = string.Equals(tenantContext.TenantRole, TenantRoles.Admin, StringComparison.OrdinalIgnoreCase);

        var query = documentsDbContext.Documents
            .AsNoTracking()
            .Include(d => d.DocumentDepartments)
            .Where(d => d.TenantId == tenantContext.TenantId && d.DeletedAt == null);

        if (!isAdmin && effectiveDepts.Count > 0)
        {
            query = query.Where(d =>
                !d.DocumentDepartments.Any() ||
                d.DocumentDepartments.Any(dd => effectiveDepts.Contains(dd.DepartmentId)));
        }
        else if (!isAdmin)
        {
            query = query.Where(d => !d.DocumentDepartments.Any());
        }

        var documents = await query
            .Select(d => new
            {
                uri = $"normora://documents/{d.Id}",
                name = d.FileName,
                description = $"Corporate Document: {d.FileName} ({d.ContentType})",
                mimeType = d.ContentType
            })
            .ToListAsync(cancellationToken);

        return McpJsonRpcResponse.Success(request.Id, new { resources = documents });
    }

    private async Task<McpJsonRpcResponse> HandleResourcesReadAsync(
        McpJsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.Params.HasValue || !request.Params.Value.TryGetProperty("uri", out var uriProp))
        {
            return McpJsonRpcResponse.Failure(request.Id, -32602, "Parameter 'uri' is required for resources/read.");
        }

        var uri = uriProp.GetString();
        if (string.IsNullOrWhiteSpace(uri) || !uri.StartsWith("normora://documents/"))
        {
            return McpJsonRpcResponse.Failure(request.Id, -32602, "Invalid URI format. Expected normora://documents/{id}");
        }

        var rawId = uri.Substring("normora://documents/".Length);
        if (!Guid.TryParse(rawId, out var documentId))
        {
            return McpJsonRpcResponse.Failure(request.Id, -32602, "Invalid document UUID in URI.");
        }

        var document = await documentsDbContext.Documents
            .AsNoTracking()
            .Include(d => d.DocumentDepartments)
            .Include(d => d.Versions)
                .ThenInclude(v => v.Chunks)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantContext.TenantId && d.DeletedAt == null, cancellationToken);

        if (document == null)
        {
            throw new BolaException();
        }

        // BOLA / Department Scope Authorization
        var isAdmin = string.Equals(tenantContext.TenantRole, TenantRoles.Admin, StringComparison.OrdinalIgnoreCase);
        var effectiveDepts = tenantContext.EffectiveDepartments.ToHashSet();
        if (!isAdmin && document.DocumentDepartments.Any() && !document.DocumentDepartments.Any(dd => effectiveDepts.Contains(dd.DepartmentId)))
        {
            throw new BolaException();
        }

        var latestVersion = document.Versions
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault();

        var extractedContent = latestVersion != null
            ? string.Join("\n\n", latestVersion.Chunks.OrderBy(c => c.ChunkIndex).Select(c => c.Content))
            : string.Empty;

        var result = new
        {
            contents = new[]
            {
                new
                {
                    uri = uri,
                    mimeType = "text/plain",
                    text = extractedContent
                }
            }
        };

        return McpJsonRpcResponse.Success(request.Id, result);
    }
}

/// <summary>
/// Model Context Protocol JSON-RPC 2.0 Request payload.
/// </summary>
public sealed record McpJsonRpcRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";

    [JsonPropertyName("id")]
    public object? Id { get; init; }

    [JsonPropertyName("method")]
    public string Method { get; init; } = string.Empty;

    [JsonPropertyName("params")]
    public JsonElement? Params { get; init; }
}

/// <summary>
/// Model Context Protocol JSON-RPC 2.0 Response payload.
/// </summary>
public sealed record McpJsonRpcResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";

    [JsonPropertyName("id")]
    public object? Id { get; init; }

    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Result { get; init; }

    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public McpJsonRpcError? Error { get; init; }

    public static McpJsonRpcResponse Success(object? id, object result) =>
        new() { Id = id, Result = result };

    public static McpJsonRpcResponse Failure(object? id, int code, string message, object? data = null) =>
        new() { Id = id, Error = new McpJsonRpcError(code, message, data) };
}

/// <summary>
/// JSON-RPC 2.0 Error object.
/// </summary>
public sealed record McpJsonRpcError(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("data")] object? Data = null);
