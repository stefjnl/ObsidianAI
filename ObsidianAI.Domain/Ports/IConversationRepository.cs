using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ObsidianAI.Domain.Entities;

namespace ObsidianAI.Domain.Ports;

/// <summary>
/// Contract for persisting and retrieving conversations from storage.
/// </summary>
public interface IConversationRepository
{
    /// <summary>
    /// Creates a new conversation in the data store.
    /// </summary>
    /// <param name="conversation">Conversation to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    Task CreateAsync(Conversation conversation, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a conversation by identifier.
    /// </summary>
    /// <param name="id">Conversation identifier.</param>
    /// <param name="includeMessages">Whether to eagerly load related messages.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching conversation or <c>null</c>.</returns>
    Task<Conversation?> GetByIdAsync(Guid id, bool includeMessages = false, CancellationToken ct = default);

    /// <summary>
    /// Retrieves conversations for a user.
    /// </summary>
    /// <param name="userId">Owning user identifier (optional until multi-user support is implemented).</param>
    /// <param name="includeArchived">Whether to include archived conversations.</param>
    /// <param name="skip">Number of results to skip for pagination.</param>
    /// <param name="take">Maximum number of results to return.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<Conversation>> GetAllAsync(string? userId, bool includeArchived, int skip, int take, CancellationToken ct = default);

    /// <summary>
    /// Persists updated conversation metadata.
    /// </summary>
    /// <param name="conversation">Conversation to update.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(Conversation conversation, CancellationToken ct = default);

    /// <summary>
    /// Archives the specified conversation.
    /// </summary>
    /// <param name="id">Conversation identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ArchiveAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Removes a conversation and all related entities.
    /// </summary>
    /// <param name="id">Conversation identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
