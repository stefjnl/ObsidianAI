

namespace ObsidianAI.Domain.Models;

/// <summary>
/// Represents the role of a participant within a conversation.
/// </summary>
public enum ParticipantRole
{
    /// <summary>
    /// A human end-user initiating or continuing the conversation.
    /// </summary>
    User,

    /// <summary>
    /// An AI assistant responding within the conversation.
    /// </summary>
    Assistant
}

/// <summary>
/// Immutable message in a conversation, independent of any provider-specific schemas.
/// </summary>
/// <param name="Role">The participant role that authored the message.</param>
/// <param name="Content">The textual content of the message.</param>
public sealed record ConversationMessage(ParticipantRole Role, string Content);