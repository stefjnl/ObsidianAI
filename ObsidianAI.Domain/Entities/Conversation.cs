using System;
using System.Collections.Generic;

namespace ObsidianAI.Domain.Entities;

/// <summary>
/// Aggregate root representing a persisted chat conversation.
/// </summary>
public class Conversation
{
    /// <summary>
    /// Unique identifier for the conversation.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Identifier of the owning user when multi-tenant scenarios are enabled.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Human friendly title derived from the first user message.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the conversation was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC timestamp when the conversation was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indicates whether the conversation has been archived by the user.
    /// </summary>
    public bool IsArchived { get; set; }

    /// <summary>
    /// LLM provider that produced the assistant responses for this conversation.
    /// </summary>
    public ConversationProvider Provider { get; set; } = ConversationProvider.Unknown;

    /// <summary>
    /// Model identifier reported by the provider when the conversation was created.
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// AgentThread identifier used by the Agent Framework to maintain multi-turn context.
    /// </summary>
    public string? ThreadId { get; set; }

    /// <summary>
    /// Messages that belong to this conversation. EF Core populates this collection.
    /// </summary>
    public List<Message> Messages { get; set; } = new();

    /// <summary>
    /// Attachments that belong to this conversation. EF Core populates this collection.
    /// </summary>
    public List<Attachment> Attachments { get; set; } = new();

    /// <summary>
    /// Concurrency token used to opt into optimistic concurrency checks.
    /// </summary>
    public byte[]? RowVersion { get; set; }

    /// <summary>
    /// Updates the last modified timestamp to the current UTC time.
    /// </summary>
    public void Touch() => UpdatedAt = DateTime.UtcNow;

    /// <summary>
    /// Adds the provided message and refreshes the updated timestamp.
    /// </summary>
    /// <param name="message">Message to associate with this conversation.</param>
    public void AddMessage(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);
        Messages.Add(message);
        Touch();
    }

    /// <summary>
    /// Adds the provided attachment and refreshes the updated timestamp.
    /// </summary>
    /// <param name="attachment">Attachment to associate with this conversation.</param>
    public void AddAttachment(Attachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        Attachments.Add(attachment);
        Touch();
    }
}
