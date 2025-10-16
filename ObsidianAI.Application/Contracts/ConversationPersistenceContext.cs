using System;
using ObsidianAI.Domain.Entities;

namespace ObsidianAI.Application.Contracts;

/// <summary>
/// Context data required to persist messages within a conversation.
/// </summary>
public sealed record ConversationPersistenceContext(
    Guid? ConversationId,
    string? UserId,
    ConversationProvider Provider,
    string ModelName,
    string? TitleSource);
