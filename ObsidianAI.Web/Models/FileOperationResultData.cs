namespace ObsidianAI.Web.Models
{
    /// <summary>
    /// Represents the result of a file operation to be displayed in the UI.
    /// </summary>
    public record FileOperationResultData
    {
        /// <summary>
        /// Whether the operation succeeded.
        /// </summary>
        public bool Success { get; init; } = true;

        /// <summary>
        /// A short label describing the operation (e.g., "File modified", "File created").
        /// </summary>
        public string Operation { get; init; } = string.Empty;

        /// <summary>
        /// The path of the file affected by the operation.
        /// </summary>
        public string FilePath { get; init; } = string.Empty;

        /// <summary>
        /// Optional custom message to display instead of "Operation: FilePath".
        /// </summary>
        public string? Message { get; init; }
    }
}