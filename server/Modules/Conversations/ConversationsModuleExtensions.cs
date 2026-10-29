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

        services.AddDbContext<ConversationsDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<IDatabaseMigrator, ConversationsDatabaseMigrator>();

        var geminiOptions = configuration.GetSection(GeminiOptions.SectionName).Get<GeminiOptions>() ?? new GeminiOptions();

        services.AddHttpClient<ITextGenerationService, GeminiTextGenerationService>(client =>
        {
            client.BaseAddress = new Uri(geminiOptions.Endpoint);
            client.Timeout = TimeSpan.FromMinutes(2);
        });

        services.AddHttpClient<IQueryRewriterService, GeminiQueryRewriterService>(client =>
        {
            client.BaseAddress = new Uri(geminiOptions.Endpoint);
            client.Timeout = TimeSpan.FromMinutes(2);
        });

        services.AddHttpClient<ISuggestionsService, GeminiSuggestionsService>(client =>
        {
            client.BaseAddress = new Uri(geminiOptions.Endpoint);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddScoped<IContextResolver, ContextResolver>();
        services.AddScoped<IConversationTurnPreparer, ConversationTurnPreparer>();
        services.AddSingleton<ITokenBudgetService, TokenBudgetService>();
        services.AddSingleton<IAutoTitleService, AutoTitleService>();
        services.AddSingleton<ConversationMetrics>();

        // Answer exporters
        services.AddSingleton<IAnswerExporter, MarkdownAnswerExporter>();
        services.AddSingleton<IAnswerExporter, PdfAnswerExporter>();
        services.AddSingleton<IAnswerExporter, DocxAnswerExporter>();

        // Agent Tools & Tool Registry
        services.AddScoped<IAgentTool, CalculatePolicyLimitTool>();
        services.AddScoped<IAgentTool, EscalateInquiryTool>();
        services.AddScoped<IAgentTool, SearchCompanyDocumentsTool>();
        services.AddScoped<IAgentToolRegistry, AgentToolRegistry>();

        return services;
    }
}
