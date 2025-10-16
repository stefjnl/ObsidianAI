using System;

namespace ObsidianAI.Domain.Entities;

/// <summary>
/// Individual planned action belonging to an action card.
/// </summary>
public class PlannedActionRecord
{
    /// <summary>
    /// Unique identifier for the planned action.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Identifier of the parent action card.
    /// </summary>
    public Guid ActionCardId { get; set; }

    /// <summary>
    /// Type of action being proposed (create, modify, etc.).
    /// </summary>
    public PlannedActionType Type { get; set; } = PlannedActionType.Other;

    /// <summary>
    /// Source path referenced by the action.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Optional destination path for actions that move or copy content.
    /// </summary>
    public string? Destination { get; set; }

    /// <summary>
    /// Description supplied by the assistant.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Operation keyword used by the API when executing the action.
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Optional content payload used for append/modify operations.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Order index for rendering actions consistently.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Navigation back to the parent action card.
    /// </summary>
    public ActionCardRecord? ActionCard { get; set; }
}
