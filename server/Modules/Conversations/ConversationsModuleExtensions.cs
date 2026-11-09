using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Normora.Modules.Conversations.Application.Exports;
using Normora.Modules.Conversations.Application.Services;
using Normora.Modules.Conversations.Infrastructure.Tools;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Conversations;

/// <summary>
/// Composition root for the Conversations module.
/// Encapsulates conversation persistence, provider-neutral LLM contracts,
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
