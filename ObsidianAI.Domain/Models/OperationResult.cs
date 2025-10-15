namespace ObsidianAI.Domain.Models
{
    /// <summary>
    /// Represents the outcome of a vault file operation.
    /// </summary>
    /// <param name="Success">Indicates whether the operation was successful.</param>
    /// <param name="Message">A descriptive message providing context on the operation result.</param>
    /// <param name="FilePath">The absolute or normalized path of the file affected by the operation.</param>
    public sealed record OperationResult(bool Success, string Message, string FilePath);
}