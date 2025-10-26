using System;
using System.Collections.Generic;

namespace ObsidianAI.Web.Models;

/// <summary>
/// Detail of a conversation
/// </summary>
public record ConversationDetail(Guid Id, string Title, DateTime CreatedAt, DateTime UpdatedAt, bool IsArchived, string Provider, string ModelName, List<ChatMessage> Messages);