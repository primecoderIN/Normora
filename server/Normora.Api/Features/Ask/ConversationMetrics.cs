using System.Diagnostics.Metrics;

namespace Normora.Api.Features.Ask;

public sealed class ConversationMetrics
{
    public const string MeterName = "Normora.Conversations";

    public Counter<long> TokensConsumed { get; }
    public Histogram<double> RagDuration { get; }
    public Counter<long> AutoTitleOperations { get; }

    public ConversationMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        TokensConsumed = meter.CreateCounter<long>("tokens.consumed", description: "Estimated LLM tokens consumed");
        RagDuration = meter.CreateHistogram<double>("rag.duration", unit: "ms", description: "RAG pipeline execution latency");
        AutoTitleOperations = meter.CreateCounter<long>("auto_title.operations", description: "Auto-title generation attempts (success or failure)");
    }
}
