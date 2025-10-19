namespace ObsidianAI.Web.Models;

/// <summary>
/// Request model for vault reorganization operations
/// </summary>
public record ReorganizeRequest
{
    /// <summary>
    /// Target file or folder path to reorganize
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Optional destination path for move operations
    /// </summary>
    public string? DestinationPath { get; init; }

    /// <summary>
    /// New name for rename operations
    /// </summary>
    public string? NewName { get; init; }

    /// <summary>
    /// Confirmation identifier to correlate with the ActionCard
    /// </summary>
    public string ConfirmationId { get; init; } = string.Empty;

    /// <summary>
    /// Operation type for the reorganization
    /// </summary>
    public string? Operation { get; init; }

    /// <summary>
    /// List of file operations to perform
    /// </summary>
    public List<FileOperation>? FileOperations { get; init; }
}

/// <summary>
/// Response model for reorganize operations
/// </summary>
public record ReorganizeResponse
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
    /// Original path of the file/folder
    /// </summary>
    public string OriginalPath { get; init; } = string.Empty;

    /// <summary>
    /// New path after reorganization
    /// </summary>
    public string? NewPath { get; init; }
}

/// <summary>
/// Request model for updating message artifacts
/// </summary>
public record ArtifactUpdateRequest
{
    /// <summary>
    /// New artifact data to associate with the message
    /// </summary>
    public MessageArtifactsUpdate? Artifacts { get; init; }
}

/// <summary>
/// Represents a single file operation within a reorganize request
/// </summary>
public class FileOperation
{
    /// <summary>
    /// Source file path
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>
    /// Destination file path
    /// </summary>
    public string? DestinationPath { get; set; }

    /// <summary>
    /// Operation type (Create, Modify, Move, Delete, etc.)
    /// </summary>
    public string Operation { get; set; } = string.Empty;
}
