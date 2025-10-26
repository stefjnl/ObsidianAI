using System;

namespace ObsidianAI.Web.Models;

/// <summary>
/// Summary of a conversation
/// </summary>
public record ConversationSummary(Guid Id, string Title, DateTime UpdatedAt, int MessageCount, string Provider, string ModelName);