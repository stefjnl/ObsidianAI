using System;

namespace ObsidianAI.Application.DTOs;

/// <summary>
/// Summary representation of a conversation for list views.
/// </summary>
public sealed record ConversationDto(
    Guid Id,
    string Title,
    DateTime UpdatedAt,
    int MessageCount,
    string Provider,
    string ModelName);
