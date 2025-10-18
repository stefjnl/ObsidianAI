using System;

namespace ObsidianAI.Domain.Entities;

/// <summary>
/// Value object representing an attachment to a conversation.
/// </summary>
public class Attachment
{
    /// <summary>
    /// Unique identifier for the attachment.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Identifier of the owning conversation.
    /// </summary>
    public Guid ConversationId { get; private set; }

    /// <summary>
    /// Original filename of the attachment.
    /// </summary>
    public string Filename { get; private set; } = string.Empty;

    /// <summary>
    /// Content of the attachment as text.
    /// </summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// File extension/type (e.g., .txt, .md, .json).
    /// </summary>
    public string FileType { get; private set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the attachment was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation back to the owning conversation.
    /// </summary>
    public Conversation? Conversation { get; set; }

    /// <summary>
    /// Concurrency token for optimistic concurrency control.
    /// </summary>
    public byte[]? RowVersion { get; set; }

    // Private constructor for EF Core
    private Attachment() { }

    /// <summary>
    /// Creates a new attachment with validation.
    /// </summary>
    /// <param name="id">Unique identifier.</param>
    /// <param name="conversationId">Owning conversation ID.</param>
    /// <param name="filename">Original filename.</param>
    /// <param name="content">Text content.</param>
    /// <param name="fileType">File extension.</param>
    public Attachment(Guid id, Guid conversationId, string filename, string content, string fileType)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty", nameof(id));
        if (conversationId == Guid.Empty) throw new ArgumentException("ConversationId cannot be empty", nameof(conversationId));
        if (string.IsNullOrWhiteSpace(filename)) throw new ArgumentException("Filename cannot be empty", nameof(filename));
        if (content.Length > 1_000_000) throw new ArgumentException("Content exceeds 1MB limit", nameof(content));

        Id = id;
        ConversationId = conversationId;
        Filename = filename;
        Content = content;
        FileType = fileType;
        CreatedAt = DateTime.UtcNow;
    }
}