using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ObsidianAI.Domain.Entities;

namespace ObsidianAI.Domain.Ports;

/// <summary>
/// Contract for managing persisted chat messages.
/// </summary>
public interface IMessageRepository
{
    /// <summary>
    /// Persists a new message.
    /// </summary>
    /// <param name="message">Message to add.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(Message message, CancellationToken ct = default);

    /// <summary>
    /// Persists multiple messages in a single batch.
    /// </summary>
    /// <param name="messages">Messages to add.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddRangeAsync(IEnumerable<Message> messages, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a message by identifier.
    /// </summary>
    /// <param name="id">Message identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Message?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all messages for a conversation ordered by timestamp.
    /// </summary>
    /// <param name="conversationId">Owning conversation identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<Message>> GetByConversationIdAsync(Guid conversationId, CancellationToken ct = default);

    /// <summary>
    /// Persists updates to a message and its related components.
    /// </summary>
    /// <param name="message">Message to update.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(Message message, CancellationToken ct = default);
}
