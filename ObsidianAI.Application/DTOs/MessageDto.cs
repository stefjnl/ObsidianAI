using System;
using System.Collections.Generic;

namespace ObsidianAI.Application.DTOs;

/// <summary>
/// Persisted message representation for API responses.
/// </summary>
public sealed record MessageDto(
    Guid Id,
    string Role,
    string Content,
    DateTime Timestamp,
    bool IsProcessing,
    int? TokenCount,
    ActionCardDto? ActionCard,
    FileOperationDto? FileOperation,
    IReadOnlyList<PlannedActionDto> PlannedActions);

/// <summary>
/// Action card data transfer object.
/// </summary>
public sealed record ActionCardDto(
    Guid Id,
    string Title,
    string Status,
    string Operation,
    string? StatusMessage,
    DateTime CreatedAt,
    DateTime? CompletedAt);

/// <summary>
/// Planned action nested within an action card.
/// </summary>
public sealed record PlannedActionDto(
    Guid Id,
    string Type,
    string Source,
    string? Destination,
    string Description,
    string Operation,
    string? Content,
    int SortOrder);

/// <summary>
/// File operation metadata associated with a message.
/// </summary>
public sealed record FileOperationDto(
    Guid Id,
    string Action,
    string FilePath,
    DateTime Timestamp);
