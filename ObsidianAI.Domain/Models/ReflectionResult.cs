using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ObsidianAI.Domain.Models;

/// <summary>
/// Result of reflecting on a file operation to determine safety and approval requirements.
/// </summary>
public class ReflectionResult
{
    /// <summary>
    /// True if the operation should be blocked entirely.
    /// </summary>
    [JsonPropertyName("shouldReject")]
    public bool ShouldReject { get; set; }

    /// <summary>
    /// True if the operation requires explicit user confirmation before proceeding.
    /// </summary>
    [JsonPropertyName("needsUserConfirmation")]
    public bool NeedsUserConfirmation { get; set; }

    /// <summary>
    /// Human-readable explanation of the reflection decision.
    /// </summary>
    [JsonPropertyName("reason")]
    public required string Reason { get; set; }

    /// <summary>
    /// Human-readable description of what the operation will do, suitable for ActionCard display.
    /// </summary>
    [JsonPropertyName("actionDescription")]
    public string? ActionDescription { get; set; }

    /// <summary>
    /// List of safety checks that were performed during reflection.
    /// </summary>
    [JsonPropertyName("safetyChecks")]
    public List<string> SafetyChecks { get; set; } = new();

    /// <summary>
    /// List of non-blocking warnings or concerns identified during reflection.
    /// </summary>
    [JsonPropertyName("warnings")]
    public List<string> Warnings { get; set; } = new();
}