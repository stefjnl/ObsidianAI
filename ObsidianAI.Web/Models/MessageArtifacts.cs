using System;
using System.Collections.Generic;

namespace ObsidianAI.Web.Models;

/// <summary>
/// Payload for updating persisted message artifacts.
/// </summary>
public sealed record MessageArtifactsUpdate(ActionCardUpdate? ActionCard, FileOperationUpdate? FileOperation);

/// <summary>
/// Action card persistence payload.
/// </summary>
public sealed record ActionCardUpdate(
    Guid? Id,
    string? Title,
    string Status,
    string? Operation,
    string? StatusMessage,
    DateTime? CreatedAt,
    DateTime? CompletedAt,
    IReadOnlyList<PlannedActionUpdate> PlannedActions);

/// <summary>
/// Planned action persistence payload.
/// </summary>
public sealed record PlannedActionUpdate(
    Guid? Id,
    string? Type,
    string? Source,
    string? Destination,
    string? Description,
    string? Operation,
    string? Content,
    int SortOrder);

/// <summary>
/// File operation persistence payload.
/// </summary>
public sealed record FileOperationUpdate(string Action, string FilePath, DateTime? Timestamp);
