using System;

namespace ObsidianAI.Web.Models;

/// <summary>
/// Represents high-level metadata about a conversation used by the UI header.
/// </summary>
public sealed record ConversationMetadata(
    Guid Id,
    string Title,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsArchived,
    string Provider,
    string ModelName,
    int MessageCount);
