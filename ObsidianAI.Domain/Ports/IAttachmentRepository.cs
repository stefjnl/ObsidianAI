using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ObsidianAI.Domain.Entities;

namespace ObsidianAI.Domain.Ports;

/// <summary>
/// Contract for persisting and retrieving attachments from storage.
/// </summary>
public interface IAttachmentRepository
{
    /// <summary>
    /// Creates a new attachment in the data store.
    /// </summary>
    /// <param name="attachment">Attachment to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    Task CreateAsync(Attachment attachment, CancellationToken ct = default);

    /// <summary>
    /// Retrieves attachments by conversation identifier.
    /// </summary>
    /// <param name="conversationId">Conversation identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching attachments.</returns>
    Task<IReadOnlyList<Attachment>> GetByConversationIdAsync(Guid conversationId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves an attachment by identifier.
    /// </summary>
    /// <param name="id">Attachment identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching attachment or <c>null</c>.</returns>
    Task<Attachment?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Removes an attachment.
    /// </summary>
    /// <param name="id">Attachment identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Contract for validating attachment properties.
/// </summary>
public interface IAttachmentValidator
{
    /// <summary>
    /// Gets the allowed file types for attachments.
    /// </summary>
    IReadOnlyList<string> AllowedFileTypes { get; }

    /// <summary>
    /// Validates if the file type is allowed.
    /// </summary>
    /// <param name="fileType">File extension to validate.</param>
    /// <returns>True if allowed.</returns>
    bool IsFileTypeAllowed(string fileType);
}