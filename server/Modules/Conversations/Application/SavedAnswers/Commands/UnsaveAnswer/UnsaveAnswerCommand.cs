using MediatR;

namespace Normora.Modules.Conversations.Application.SavedAnswers.Commands;

/// <summary>
/// Removes a previously saved answer for the current user.
/// Idempotent — silently succeeds if the saved answer does not exist.
/// </summary>
public record UnsaveAnswerCommand(Guid MessageId) : IRequest;

