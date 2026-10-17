using MediatR;
using Normora.Modules.Conversations.Application.Services;
using Normora.Shared.Interfaces;

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
    ISuggestionsService suggestionsService,
    ITenantContext tenantContext,
    IMediator mediator,
    ICacheService cache) : IRequestHandler<GetSuggestedQuestionsQuery, IReadOnlyList<SuggestedQuestionDto>>
{
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

        var effectiveDepts = tenantContext.EffectiveDepartments.OrderBy(d => d).ToArray();
        var deptKey = effectiveDepts.Length == 0 ? "company-wide" : string.Join("-", effectiveDepts);
        var cacheKey = $"suggested-questions:{tenantContext.TenantId.Value}:{deptKey}";

        return await cache.GetOrCreateAsync(cacheKey, async ct =>
        {
            IReadOnlyList<string> documentNames;

            try
            {
                documentNames = await mediator.Send(
                    new Normora.Shared.Contracts.Documents.GetAccessibleDocumentFileNamesQuery(effectiveDepts, Limit: 20),
                    ct);
            }
            catch (Exception)
            {
                return FallbackSuggestions;
            }

            if (documentNames.Count == 0)
            {
                return FallbackSuggestions;
            }

            var questions = await suggestionsService.GenerateSuggestionsAsync(documentNames, ct);

            if (questions.Count == 0)
            {
                return FallbackSuggestions;
            }

            return (IReadOnlyList<SuggestedQuestionDto>)questions
                .Select((q, i) => new SuggestedQuestionDto(q, Icons[i % Icons.Length]))
                .ToList()
                .AsReadOnly();
        }, TimeSpan.FromHours(6), cancellationToken);
    }
}
