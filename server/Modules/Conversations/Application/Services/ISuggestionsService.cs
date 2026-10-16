namespace Normora.Modules.Conversations.Application.Services;

/// <summary>
/// Generates context-aware suggested questions for an employee based on the
/// document knowledge base available to them (company-wide + effective departments).
/// </summary>
public interface ISuggestionsService
{
    /// <summary>
    /// Given a list of document file names, returns a set of naturally phrased
    /// questions an employee might ask about that knowledge base.
    /// </summary>
    /// <param name="documentNames">File names of accessible, Ready documents (max ~20).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of suggested question strings, or an empty list on failure.</returns>
    Task<IReadOnlyList<string>> GenerateSuggestionsAsync(
        IReadOnlyList<string> documentNames,
        CancellationToken cancellationToken = default);
}
