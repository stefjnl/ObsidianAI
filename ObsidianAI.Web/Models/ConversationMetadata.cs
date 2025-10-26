using System;

namespace ObsidianAI.Web.Models;

/// <summary>
/// Metadata of a conversation
/// </summary>
public record ConversationMetadata(Guid Id, string Title, DateTime CreatedAt, DateTime UpdatedAt, bool IsArchived, string Provider, string ModelName, int MessageCount);