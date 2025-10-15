using ObsidianAI.Web.Models;

namespace ObsidianAI.Web.Models;

/// <summary>
/// Request model for single-file modify operations (append/modify/delete/create).
/// </summary>
public record ModifyRequest
{
    /// <summary>
    /// Operation to perform: append, modify, delete, create, patch, write
    /// </summary>
    public string Operation { get; init; } = string.Empty;

    /// <summary>
    /// Target file path
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Content payload for append/modify/create operations
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Confirmation identifier to correlate with the ActionCard
    /// </summary>
    public string ConfirmationId { get; init; } = string.Empty;
}

/// <summary>
/// Response model for modify operations.
/// </summary>
public record ModifyResponse
{
    /// <summary>
    /// Whether the operation succeeded
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Human-readable status message
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Path of the file that was operated on
    /// </summary>
    public string FilePath { get; init; } = string.Empty;
}