namespace Normora.Modules.Conversations.Domain;

/// <summary>
/// Rating sentiment for assistant answers.
/// </summary>
public enum MessageFeedbackRating
{
    /// <summary>Helpful response meeting user expectations.</summary>
    Positive = 1,

    /// <summary>Inaccurate, incomplete, or unhelpful response.</summary>
    Negative = 2
}

