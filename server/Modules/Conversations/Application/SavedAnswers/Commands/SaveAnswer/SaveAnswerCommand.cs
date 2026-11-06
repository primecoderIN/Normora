using MediatR;

namespace Normora.Modules.Conversations.Application.SavedAnswers.Commands;

/// <summary>
/// Saves an assistant message as a bookmark for the current user.
/// Idempotent — silently succeeds if the message is already saved.
/// </summary>
public record SaveAnswerCommand(Guid MessageId) : IRequest<Guid>;

