using MediatR;
using Normora.Modules.Conversations.Domain;
using System;

namespace Normora.Modules.Conversations.Application.Conversations.Commands;

/// <summary>
/// Command to register user feedback (thumbs up / thumbs down rating and comment) on an AI assistant response.
/// Handled by <see cref="SubmitFeedbackCommandHandler"/>.
/// </summary>
/// <remarks>
/// Evaluation & Analytics:
/// - Verifies that the message belongs to a conversation owned by the caller (BOLA protection).
/// - Idempotent: upserts an existing feedback record if previously rated.
/// - Metric pipelines aggregate feedback ratings to compute Answer Quality scores on the employer dashboard.
/// </remarks>
/// <param name="ConversationId">Conversation identifier containing the message.</param>
/// <param name="MessageId">Specific assistant message being rated.</param>
/// <param name="Rating">Positive (1) or negative (-1) rating sentiment.</param>
/// <param name="Comment">Optional user comment or reason for rating.</param>
public sealed record SubmitFeedbackCommand(
    Guid ConversationId,
    Guid MessageId,
    MessageFeedbackRating Rating,
    string? Comment) : IRequest;

