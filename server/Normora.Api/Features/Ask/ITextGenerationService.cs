namespace Normora.Api.Features.Ask;

public interface ITextGenerationService
{
    bool IsConfigured { get; }
    Task<string> GenerateGroundedAnswerAsync(
        string question,
        IReadOnlyList<AskSource> sources,
        CancellationToken cancellationToken = default);
}

public sealed record AskSource(string FileName, int ChunkIndex, string Content);