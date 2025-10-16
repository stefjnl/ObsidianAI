using System;

namespace ObsidianAI.Domain.Entities;

/// <summary>
/// Persisted file operation metadata linked to a message.
/// </summary>
public class FileOperationRecord
{
    /// <summary>
    /// Unique identifier for the file operation record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Identifier of the message this file operation belongs to.
    /// </summary>
    public Guid MessageId { get; set; }

    /// <summary>
    /// Action performed on the file (created, modified, etc.).
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// File system path impacted by the operation.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the operation completed.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation back to the owning message.
    /// </summary>
    public Message? Message { get; set; }
}
