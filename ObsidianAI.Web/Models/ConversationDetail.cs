using System;
using System.Collections.Generic;

namespace ObsidianAI.Web.Models;

/// <summary>
/// Represents a fully loaded conversation with its persisted messages.
/// </summary>
public sealed record ConversationDetail(
    Guid Id,
    string Title,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsArchived,
    string Provider,
    string ModelName,
    IReadOnlyList<ChatMessage> Messages);
