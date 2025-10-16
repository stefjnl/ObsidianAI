using System;
using System.Collections.Generic;

namespace ObsidianAI.Domain.Entities;

/// <summary>
/// Persisted representation of an assistant-generated action card.
/// </summary>
public class ActionCardRecord
{
    /// <summary>
    /// Unique identifier for the action card.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Identifier of the message associated with this action card.
    /// </summary>
    public Guid MessageId { get; set; }

    /// <summary>
    /// Display title presented to the user.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Status of the action card workflow.
    /// </summary>
    public ActionCardStatus Status { get; set; } = ActionCardStatus.Pending;

    /// <summary>
    /// Optional descriptive status message.
    /// </summary>
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Operation grouping for the set of planned actions.
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the action card was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC timestamp when the action card was completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Planned actions associated with this card.
    /// </summary>
    public List<PlannedActionRecord> PlannedActions { get; set; } = new();

    /// <summary>
    /// Navigation back to the owning message.
    /// </summary>
    public Message? Message { get; set; }
}
