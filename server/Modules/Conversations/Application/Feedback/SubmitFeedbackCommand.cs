using MediatR;
using Normora.Modules.Conversations.Domain;
using System;

namespace Normora.Modules.Conversations.Application.Feedback;

public sealed record SubmitFeedbackCommand(
    Guid ConversationId,
    Guid MessageId,
    MessageFeedbackRating Rating,
    string? Comment) : IRequest;
