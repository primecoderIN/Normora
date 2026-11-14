using System.Text.Json;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations.Infrastructure.Tools;

/// <summary>
/// Autonomous agent tool that presents an interactive dynamic intake form in chat when required parameters
/// are missing from the user's initial prompt (e.g., creating departments, inviting employees, creating user groups).
/// </summary>
public sealed class WorkspaceRequestFormTool : IAgentTool
{
    public string Name => "workspace_request_form";

    public string Description =>
        "Presents an interactive intake form to the user in chat when required parameters are missing to execute a workspace action (such as creating departments, inviting employees, or user group configuration).";

    public ToolCategory Category => ToolCategory.ReadOnly;

    public bool RequiresApproval => false;

    public ToolChipDescriptor ChipDescriptor => new(
        StartLabel: "Preparing intake form...",
        CompletedLabel: "Intake form displayed",
        FailedLabel: "Failed to generate intake form");

    public object ParameterSchema => new
    {
        type = "object",
        properties = new
        {
            actionTarget = new
            {
                type = "string",
                description = "The target tool name that will be invoked once the form is submitted (e.g. workspace_create_department, workspace_invite_employee)."
            },
            title = new
            {
                type = "string",
                description = "The human-readable title of the form (e.g. 'Create Department', 'Invite Team Member')."
            },
            description = new
            {
                type = "string",
                description = "Contextual instructions displayed to the user explaining the required inputs."
            },
            fieldsJson = new
            {
                type = "string",
                description = "JSON array of form fields. Each field must have 'fieldId', 'label', 'type' ('text'|'number'|'email'|'select'|'textarea'), and optional 'required', 'placeholder', 'options' (array of {label, value})."
            }
        },
        required = new[] { "actionTarget", "title", "description", "fieldsJson" }
    };

    public Task<AgentToolResult> ExecuteAsync(
        string argumentsJson,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
            var root = doc.RootElement;

            var actionTarget = root.TryGetProperty("actionTarget", out var targetProp) ? targetProp.GetString() ?? "" : "";
            var title = root.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? "Workspace Form" : "Workspace Form";
            var description = root.TryGetProperty("description", out var descProp) ? descProp.GetString() ?? "" : "";
            var fieldsJson = root.TryGetProperty("fieldsJson", out var fieldsProp) ? fieldsProp.GetString() ?? "[]" : "[]";

            var structured = new
            {
                actionTarget,
                title,
                description,
                fieldsJson
            };

            var outputJson = JsonSerializer.Serialize(structured, new JsonSerializerOptions { WriteIndented = true });
            return Task.FromResult(new AgentToolResult(true, outputJson, StructuredData: structured));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new AgentToolResult(false, string.Empty, ex.Message));
        }
    }
}

