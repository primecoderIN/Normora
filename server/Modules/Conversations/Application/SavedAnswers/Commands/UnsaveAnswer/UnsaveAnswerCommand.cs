using MediatR;

namespace Normora.Modules.Conversations.Application.SavedAnswers.Commands;

/// <summary>
/// Command to remove a previously bookmarked assistant answer for the authenticated employee.
/// Handled by <see cref="UnsaveAnswerCommandHandler"/>.
/// </summary>
/// <remarks>
/// Idempotent Behavior:
/// - Locates bookmark matching MessageId and the caller's external user identifier.
/// - Silently succeeds if no bookmark exists, preventing redundant errors on rapid UI toggling.
/// </remarks>
/// <param name="MessageId">The message identifier being un-bookmarked.</param>
public record UnsaveAnswerCommand(Guid MessageId) : IRequest;

