using System.Diagnostics.Metrics;

namespace Normora.Modules.Conversations.Application.Services;

/// <summary>
/// OpenTelemetry metrics recorder for conversational RAG interactions, tracking token consumption and execution latency.
/// </summary>
public sealed class ConversationMetrics(IMeterFactory meterFactory)
{
    public const string MeterName = "Normora.Conversations";

    private readonly Meter _meter = meterFactory.Create(MeterName);
    private Counter<long>? _tokensConsumed;
    private Histogram<double>? _ragDuration;
    private Counter<long>? _autoTitleOperations;

    /// <summary>Cumulative counter tracking LLM prompt and completion tokens.</summary>
    public Counter<long> TokensConsumed => _tokensConsumed ??= _meter.CreateCounter<long>("tokens.consumed", description: "Estimated LLM tokens consumed");

    /// <summary>Histogram tracking total RAG round-trip duration in milliseconds.</summary>
    public Histogram<double> RagDuration => _ragDuration ??= _meter.CreateHistogram<double>("rag.duration", unit: "ms", description: "RAG pipeline execution latency");

    /// <summary>Counter tracking automatic title generation background jobs.</summary>
    public Counter<long> AutoTitleOperations => _autoTitleOperations ??= _meter.CreateCounter<long>("auto_title.operations", description: "Auto-title generation attempts (success or failure)");
}
