using System;

namespace ObsidianAI.Web.Models;

/// <summary>
/// Summarizes a conversation for display in the sidebar list.
/// </summary>
public sealed record ConversationSummary(
    Guid Id,
    string Title,
    DateTime UpdatedAt,
    int MessageCount,
    string Provider,
    string ModelName);
