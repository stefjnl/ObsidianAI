using System;

namespace ObsidianAI.Domain.Entities;

/// <summary>
/// Persisted chat message belonging to a conversation.
/// </summary>
public class Message
{
    /// <summary>
    /// Unique identifier of the message.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Identifier of the owning conversation.
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// Role that produced the message content.
    /// </summary>
    public MessageRole Role { get; set; }

    /// <summary>
    /// Body of the message.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the message was recorded.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Number of tokens consumed to generate the assistant response.
    /// </summary>
    public int? TokenCount { get; set; }

    /// <summary>
    /// Indicates whether the message is still awaiting completion.
    /// </summary>
    public bool IsProcessing { get; set; }

    /// <summary>
    /// Related action card, when the assistant proposes changes.
    /// </summary>
    public ActionCardRecord? ActionCard { get; set; }

    /// <summary>
    /// Related file operation generated during the conversation.
    /// </summary>
    public FileOperationRecord? FileOperation { get; set; }

    /// <summary>
    /// Navigation back to the owning conversation.
    /// </summary>
    public Conversation? Conversation { get; set; }

    /// <summary>
    /// Concurrency token for optimistic concurrency control.
    /// </summary>
    public byte[]? RowVersion { get; set; }
}
