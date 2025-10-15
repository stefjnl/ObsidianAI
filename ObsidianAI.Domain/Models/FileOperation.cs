namespace ObsidianAI.Domain.Models
{
    /// <summary>
    /// Represents a structured file operation extracted from model text.
    /// </summary>
    /// <param name="Action">The action to perform (e.g., "append", "patch", "delete", "create").</param>
    /// <param name="FilePath">The resolved or normalized file path targeted by the operation.</param>
    public sealed record FileOperation(string Action, string FilePath);
}