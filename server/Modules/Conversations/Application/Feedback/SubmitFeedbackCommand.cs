using MediatR;
using Normora.Modules.Conversations.Domain;
using System;

namespace Normora.Modules.Conversations.Application.Feedback;

/// <summary>
/// Command to register user feedback (thumbs up / thumbs down / comment) for an assistant message.
/// </summary>
/// <param name="ConversationId">Conversation identifier containing the message.</param>
/// <param name="MessageId">Specific message being rated.</param>
/// <param name="Rating">Positive or negative rating sentiment.</param>
/// <param name="Comment">Optional user comment or reason for rating.</param>
public sealed record SubmitFeedbackCommand(
    Guid ConversationId,
    Guid MessageId,
    MessageFeedbackRating Rating,
    string? Comment) : IRequest;

