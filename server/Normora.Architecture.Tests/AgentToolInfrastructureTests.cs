using MediatR;
using Normora.Modules.Conversations.Application.Services;
using Normora.Modules.Conversations.Infrastructure.Cards;
using Normora.Modules.Conversations.Infrastructure.Tools;
using Normora.Modules.Documents.Infrastructure.Tools;
using Normora.Modules.Tenants.Infrastructure.Tools;
using Normora.Shared.Interfaces;
using Xunit;

namespace Normora.Architecture.Tests;

public sealed class AgentToolInfrastructureTests
{
    [Fact]
    public void CalculatePolicyLimitTool_MustHaveCorrectMetadata()
    {
        var tool = new CalculatePolicyLimitTool();

        Assert.Equal("calculate_policy_limit", tool.Name);
        Assert.Equal(ToolCategory.ReadOnly, tool.Category);
        Assert.False(tool.RequiresApproval);
        Assert.NotNull(tool.ChipDescriptor);
        Assert.Equal("Calculating corporate policy limits...", tool.ChipDescriptor.StartLabel);
        Assert.Equal("Policy limit calculation completed", tool.ChipDescriptor.CompletedLabel);
    }

    [Fact]
    public async Task CalculatePolicyLimitTool_ShouldProduceStructuredDataAndOutput()
    {
        var tool = new CalculatePolicyLimitTool();
        var context = new ToolExecutionContext(Guid.NewGuid(), "Acme Corp", "user1", "user@acme.com", "Test User", null);

        var result = await tool.ExecuteAsync("{\"policyType\":\"hotel\",\"tierOrLocation\":\"tier_1\",\"days\":2}", context);

        Assert.True(result.Success);
        Assert.Contains("Hotel Lodging Allowance", result.Output);
        Assert.NotNull(result.StructuredData);
        Assert.False(result.ChangedData);
    }

    [Fact]
    public void DocumentsDeleteTool_MustBeDestructiveAndRequireApproval()
    {
        var mediator = new DummyMediator();
        var tool = new DocumentsDeleteTool(mediator);

        Assert.Equal("documents_delete", tool.Name);
        Assert.Equal(ToolCategory.Destructive, tool.Category);
        Assert.True(tool.RequiresApproval);
        Assert.NotNull(tool.ChipDescriptor);
        Assert.Equal("Deleting company knowledge document...", tool.ChipDescriptor.StartLabel);
    }

    [Fact]
    public void WorkspaceTools_ShouldDeclareCorrectCategories()
    {
        var mediator = new DummyMediator();
        var listDeptsTool = new WorkspaceListDepartmentsTool(mediator);
        var createDeptTool = new WorkspaceCreateDepartmentTool(mediator);

        Assert.Equal(ToolCategory.ReadOnly, listDeptsTool.Category);
        Assert.False(listDeptsTool.RequiresApproval);

        Assert.Equal(ToolCategory.Mutation, createDeptTool.Category);
        Assert.False(createDeptTool.RequiresApproval);
    }

    [Fact]
    public void CalculatePolicyLimitCardBuilder_ShouldBuildCardForSuccessfulCalculation()
    {
        var builder = new CalculatePolicyLimitCardBuilder();
        Assert.True(builder.CanHandle("calculate_policy_limit"));
        Assert.False(builder.CanHandle("unknown_tool"));

        var output = "{\"policy\":\"Hotel Lodging Allowance\",\"tier\":\"Tier 1 (High-Cost Metro)\",\"totalMaxAllowanceUsd\":550,\"nightlyCapUsd\":275,\"guideline\":\"Excludes incidentals.\"}";
        var toolResult = new AgentToolResult(true, output);

        var card = builder.BuildCard("calculate_policy_limit", "{}", toolResult);

        Assert.NotNull(card);
        Assert.Equal("policy_limit_card", card.ActionType);
        Assert.Equal("Hotel Lodging Allowance", card.Title);
        Assert.Equal("Apply Limit", card.ConfirmButtonText);
        Assert.Equal("Dismiss", card.CancelButtonText);
        Assert.Equal("Tier 1 (High-Cost Metro)", card.Parameters["Location Tier"]);
        Assert.Equal("$550", card.Parameters["Total Max Allowance"]);
    }

    [Fact]
    public void EscalateInquiryCardBuilder_ShouldBuildCardForEscalation()
    {
        var builder = new EscalateInquiryCardBuilder();
        Assert.True(builder.CanHandle("escalate_hr_inquiry"));

        var args = "{\"inquiry\":\"When does paternity leave start?\",\"category\":\"benefits\",\"urgency\":\"high\"}";
        var toolResult = new AgentToolResult(true, "{\"status\":\"escalated\"}", StructuredData: new { }, ChangedData: true);

        var card = builder.BuildCard("escalate_hr_inquiry", args, toolResult);

        Assert.NotNull(card);
        Assert.Equal("hr_escalation_card", card.ActionType);
        Assert.Equal("HR Administration Escalation", card.Title);
        Assert.Equal("When does paternity leave start?", card.Parameters["Inquiry Subject"]);
        Assert.Equal("benefits", card.Parameters["Department Category"]);
        Assert.Equal("high", card.Parameters["Urgency Level"]);
    }

    [Fact]
    public void DocumentsSearchCardBuilder_ShouldBuildCardForSearchResults()
    {
        var builder = new DocumentsSearchCardBuilder();
        Assert.True(builder.CanHandle("documents_search"));

        var output = "{\"query\":\"remote work policy\",\"totalMatches\":2,\"results\":[{\"documentId\":\"11111111-1111-1111-1111-111111111111\",\"fileName\":\"Handbook.pdf\",\"snippet\":\"Remote work is permitted 2 days per week.\",\"score\":0.92}]}";
        var toolResult = new AgentToolResult(true, output);

        var card = builder.BuildCard("documents_search", "{\"query\":\"remote work policy\"}", toolResult);

        Assert.NotNull(card);
        Assert.Equal("document_search_card", card.ActionType);
        Assert.Equal("Knowledge Base Search: \"remote work policy\"", card.Title);
        Assert.Equal("2 documents", card.Parameters["Total Matches"]);
        Assert.Equal("Handbook.pdf (92%)", card.Parameters["Top Result"]);
    }

    [Fact]
    public void WorkspaceDepartmentsCardBuilder_ShouldBuildCardForDepartmentList()
    {
        var builder = new WorkspaceDepartmentsCardBuilder();
        Assert.True(builder.CanHandle("workspace_list_departments"));

        var output = "{\"tenantName\":\"Acme Corp\",\"totalDepartments\":3,\"departments\":[{\"name\":\"Engineering\",\"code\":\"ENG\"},{\"name\":\"Sales\",\"code\":\"SLS\"}]}";
        var toolResult = new AgentToolResult(true, output);

        var card = builder.BuildCard("workspace_list_departments", "{}", toolResult);

        Assert.NotNull(card);
        Assert.Equal("workspace_departments_card", card.ActionType);
        Assert.Equal("Acme Corp Departments", card.Title);
        Assert.Equal("3", card.Parameters["Total Departments"]);
        Assert.Equal("Engineering (ENG), Sales (SLS)", card.Parameters["Sample Departments"]);
    }

    [Fact]
    public void ActionCardFactory_ShouldDelegateToMatchingBuilder()
    {
        var builders = new IActionCardBuilder[]
        {
            new CalculatePolicyLimitCardBuilder(),
            new EscalateInquiryCardBuilder(),
            new DocumentsSearchCardBuilder(),
            new WorkspaceDepartmentsCardBuilder()
        };
        var factory = new ActionCardFactory(builders);

        var output = "{\"policy\":\"Hotel Lodging Allowance\",\"tier\":\"Tier 1 (High-Cost Metro)\",\"totalMaxAllowanceUsd\":550}";
        var card = factory.CreateCard("calculate_policy_limit", "{}", new AgentToolResult(true, output));

        Assert.NotNull(card);
        Assert.Equal("policy_limit_card", card.ActionType);

        var nonExistentCard = factory.CreateCard("non_existent_tool", "{}", new AgentToolResult(true, "{}"));
        Assert.Null(nonExistentCard);
    }

    private sealed class DummyMediator : IMediator
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest =>
            throw new NotImplementedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task Publish(object notification, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification =>
            Task.CompletedTask;

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}

