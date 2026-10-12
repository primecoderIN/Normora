using MediatR;
using Microsoft.EntityFrameworkCore;
using Normora.Modules.Conversations.Application.Services;
using Normora.Modules.Conversations.Persistence;
using Normora.Shared.Interfaces;
using Npgsql;

namespace Normora.Modules.Conversations.Application.Queries;

// ─── Query & DTO ──────────────────────────────────────────────────────────────

public record GetSuggestedQuestionsQuery : IRequest<IReadOnlyList<SuggestedQuestionDto>>;

/// <summary>
/// A single AI-generated question suggestion for the empty-state of the chat UI.
/// </summary>
/// <param name="Question">The naturally-phrased question string.</param>
/// <param name="IconClass">A PrimeIcons CSS class to render alongside the card (e.g. "pi-file-edit").</param>
public record SuggestedQuestionDto(string Question, string IconClass);

// ─── Handler ──────────────────────────────────────────────────────────────────

/// <summary>
/// Fetches document names accessible to the authenticated user (company-wide + effective departments),
/// then asks Gemini to generate 5 relevant suggested questions.
/// Falls back to 3 static questions if no documents exist or if the AI call fails.
/// </summary>
public class GetSuggestedQuestionsQueryHandler(
    ConversationsDbContext context,
    ISuggestionsService suggestionsService,
    ITenantContext tenantContext) : IRequestHandler<GetSuggestedQuestionsQuery, IReadOnlyList<SuggestedQuestionDto>>
{
    // Status enum value 2 = Ready (DocumentStatus.Ready in the Documents module)
    private const int DocumentStatusReady = 2;

    // Static icon classes cycled over the returned suggestions
    private static readonly string[] Icons =
    [
        "pi-file-edit",
        "pi-users",
        "pi-wallet",
        "pi-calendar",
        "pi-shield"
    ];

    // Fallback static suggestions shown when no documents exist yet or Gemini is unavailable.
    private static readonly IReadOnlyList<SuggestedQuestionDto> FallbackSuggestions =
    [
        new("How do I request time off?", "pi-calendar"),
        new("What is our remote work policy?", "pi-users"),
        new("How do I submit expenses?", "pi-wallet")
    ];

    public async Task<IReadOnlyList<SuggestedQuestionDto>> Handle(
        GetSuggestedQuestionsQuery request,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.IsTenantResolved || !tenantContext.TenantId.HasValue)
        {
            return FallbackSuggestions;
        }

        var tenantId = tenantContext.TenantId.Value;
        var effectiveDepts = tenantContext.EffectiveDepartments.ToArray();

        // Cross-module raw SQL: Read document filenames from the Documents module's tables.
        // This is safe in a modular monolith that shares a single database — raw SQL is the
        // intentional cross-module boundary here (no EF entity references across modules).
        //
        // Filter: tenant-owned, not soft-deleted, active version is Ready, and scoped to
        // Company Wide (no dept assignments) OR the user's effective departments.
        // Cap at 20 names to keep the Gemini prompt small and cheap.

        string sql;
        List<string> documentNames;

        try
        {
            if (effectiveDepts.Length == 0)
            {
                // No department assignments → only company-wide docs (those with no dept rows)
                sql = """
                    SELECT DISTINCT d."FileName"
                    FROM "Documents" d
                    JOIN "DocumentVersions" dv ON dv."DocumentId" = d."Id" AND dv."IsActive" = true
                    LEFT JOIN "DocumentDepartments" dd ON dd."DocumentId" = d."Id"
                    WHERE d."TenantId" = @tenantId
                      AND d."DeletedAt" IS NULL
                      AND dv."Status" = @status
                      AND dd."DepartmentId" IS NULL
                    LIMIT 20
                    """;

                documentNames = await context.Database
                    .SqlQueryRaw<string>(sql,
                        new NpgsqlParameter("tenantId", tenantId),
                        new NpgsqlParameter("status", DocumentStatusReady))
                    .ToListAsync(cancellationToken);
            }
            else
            {
                // Has departments → company-wide + their departments
                sql = """
                    SELECT DISTINCT d."FileName"
                    FROM "Documents" d
                    JOIN "DocumentVersions" dv ON dv."DocumentId" = d."Id" AND dv."IsActive" = true
                    LEFT JOIN "DocumentDepartments" dd ON dd."DocumentId" = d."Id"
                    WHERE d."TenantId" = @tenantId
                      AND d."DeletedAt" IS NULL
                      AND dv."Status" = @status
                      AND (dd."DepartmentId" IS NULL OR dd."DepartmentId" = ANY(@deptIds))
                    LIMIT 20
                    """;

                documentNames = await context.Database
                    .SqlQueryRaw<string>(sql,
                        new NpgsqlParameter("tenantId", tenantId),
                        new NpgsqlParameter("status", DocumentStatusReady),
                        new NpgsqlParameter("deptIds", effectiveDepts))
                    .ToListAsync(cancellationToken);
            }
        }
        catch (Exception)
        {
            // If the cross-module SQL fails for any reason, silently return the fallback.
            return FallbackSuggestions;
        }

        if (documentNames.Count == 0)
        {
            return FallbackSuggestions;
        }

        var questions = await suggestionsService.GenerateSuggestionsAsync(documentNames, cancellationToken);

        if (questions.Count == 0)
        {
            return FallbackSuggestions;
        }

        return questions
            .Select((q, i) => new SuggestedQuestionDto(q, Icons[i % Icons.Length]))
            .ToList()
            .AsReadOnly();
    }
}
