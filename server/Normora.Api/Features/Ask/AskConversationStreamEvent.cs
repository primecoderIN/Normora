using System.Text.Json.Serialization;

namespace Normora.Api.Features.Ask;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CitationsReadyEvent), "citations")]
[JsonDerivedType(typeof(TextChunkEvent), "text")]
[JsonDerivedType(typeof(StreamFinishedEvent), "finished")]
public abstract record AskConversationStreamEvent;

public sealed record CitationsReadyEvent(
    Guid UserMessageId,
    Guid AssistantMessageId,
    IReadOnlyList<AskConversationCitation> Sources) : AskConversationStreamEvent;

public sealed record TextChunkEvent(string Text) : AskConversationStreamEvent;

public sealed record StreamFinishedEvent() : AskConversationStreamEvent;
