using System;
using System.Collections.Generic;

namespace ObsidianAI.Application.DTOs;

/// <summary>
/// Detailed conversation view containing messages and metadata.
/// </summary>
public sealed record ConversationDetailDto(
    Guid Id,
    string Title,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsArchived,
    string Provider,
    string ModelName,
    IReadOnlyList<MessageDto> Messages);
