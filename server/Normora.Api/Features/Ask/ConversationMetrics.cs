using System.Diagnostics.Metrics;

namespace Normora.Api.Features.Ask;

public sealed class ConversationMetrics(IMeterFactory meterFactory)
{
    public const string MeterName = "Normora.Conversations";

    private readonly Meter _meter = meterFactory.Create(MeterName);
    private Counter<long>? _tokensConsumed;
    private Histogram<double>? _ragDuration;
    private Counter<long>? _autoTitleOperations;

    public Counter<long> TokensConsumed => _tokensConsumed ??= _meter.CreateCounter<long>("tokens.consumed", description: "Estimated LLM tokens consumed");
    public Histogram<double> RagDuration => _ragDuration ??= _meter.CreateHistogram<double>("rag.duration", unit: "ms", description: "RAG pipeline execution latency");
    public Counter<long> AutoTitleOperations => _autoTitleOperations ??= _meter.CreateCounter<long>("auto_title.operations", description: "Auto-title generation attempts (success or failure)");
}
