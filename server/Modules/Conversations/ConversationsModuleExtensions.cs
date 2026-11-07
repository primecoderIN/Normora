using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Normora.Modules.Conversations.Application.Exports;
using Normora.Modules.Conversations.Application.Services;
using Normora.Modules.Conversations.Infrastructure.Llm;
using Normora.Modules.Conversations.Infrastructure.Tools;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Interfaces;
using Normora.Shared.Options;

namespace Normora.Modules.Conversations;

/// <summary>
/// Composition root for the Conversations module.
/// Encapsulates conversation database context, LLM text generation/rewriter/suggestions,
/// context budgeting, exporters, and agent tools.
/// </summary>
public static class ConversationsModuleExtensions
{
    public static IServiceCollection AddConversationsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // Module-Private DbContext:
        // Registers ConversationsDbContext strictly for the 'conversations' PostgreSQL schema.
        services.AddDbContext<ConversationsDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<IDatabaseMigrator, ConversationsDatabaseMigrator>();

        var geminiOptions = configuration.GetSection(GeminiOptions.SectionName).Get<GeminiOptions>() ?? new GeminiOptions();

        // LLM Text Generation Client (Google Gemini 1.5 Flash):
        // Configured with a 2-minute timeout for conversational streaming and autonomous tool execution.
        services.AddHttpClient<ITextGenerationService, GeminiTextGenerationService>(client =>
        {
            client.BaseAddress = new Uri(geminiOptions.Endpoint);
            client.Timeout = TimeSpan.FromMinutes(2);
        });

        // LLM Query Rewriter Client:
        // Reformulates contextual followup questions into standalone search queries.
        services.AddHttpClient<IQueryRewriterService, GeminiQueryRewriterService>(client =>
        {
            client.BaseAddress = new Uri(geminiOptions.Endpoint);
            client.Timeout = TimeSpan.FromMinutes(2);
        });

        // LLM Suggested Followup Prompts Client:
        // Fast 30-second timeout generating 3 dynamic followup question chips based on accessible documents.
        services.AddHttpClient<ISuggestionsService, GeminiSuggestionsService>(client =>
        {
            client.BaseAddress = new Uri(geminiOptions.Endpoint);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Context Assembly & Token Budgeting:
        services.AddScoped<IContextResolver, ContextResolver>();
        services.AddScoped<IConversationTurnPreparer, ConversationTurnPreparer>();
        services.AddSingleton<ITokenBudgetService, TokenBudgetService>();
        services.AddSingleton<IAutoTitleService, AutoTitleService>();
        services.AddSingleton<ConversationMetrics>();

        // Multi-Format Saved Answer Exporters (Markdown, QuestPDF, OpenXML DOCX):
        services.AddSingleton<IAnswerExporter, MarkdownAnswerExporter>();
        services.AddSingleton<IAnswerExporter, PdfAnswerExporter>();
        services.AddSingleton<IAnswerExporter, DocxAnswerExporter>();

        // Agent Tools & Cross-Module Tool Registry
        services.AddScoped<IAgentTool, CalculatePolicyLimitTool>();
        services.AddScoped<IAgentTool, EscalateInquiryTool>();
        services.AddScoped<IAgentTool, SearchCompanyDocumentsTool>();
        services.AddScoped<IAgentToolRegistry, AgentToolRegistry>();

        return services;
    }
}
