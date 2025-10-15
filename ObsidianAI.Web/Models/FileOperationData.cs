namespace ObsidianAI.Web.Models
{
    /// <summary>
    /// Represents the result of a file operation to be displayed in the UI.
    /// </summary>
    public record FileOperationData
    {
        /// <summary>
        /// The action performed on the file (e.g., "created", "modified", "deleted").
        /// </summary>
        public string Action { get; init; } = string.Empty;

        /// <summary>
        /// The path of the file affected by the operation.
        /// </summary>
        public string FilePath { get; init; } = string.Empty;
    }
}