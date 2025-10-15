namespace ObsidianAI.Domain.Ports
{
    using System.Threading;
    using System.Threading.Tasks;
    using ObsidianAI.Domain.Models;

    /// <summary>
    /// Abstracts vault operations (append/patch/delete/create).
    /// Implementations execute file mutations against a backing store (e.g., disk, remote vault).
    /// </summary>
    public interface IVaultToolExecutor
    {
        /// <summary>
        /// Appends content to an existing file at the specified path.
        /// </summary>
        /// <param name="filePath">The normalized vault file path.</param>
        /// <param name="content">The content to append.</param>
        /// <param name="ct">A cancellation token to cancel the operation.</param>
        /// <returns>An <see cref="OperationResult"/> describing the outcome.</returns>
        Task<OperationResult> AppendAsync(string filePath, string content, CancellationToken ct = default);

        /// <summary>
        /// Applies a patch operation to the file at the specified path.
        /// </summary>
        /// <param name="filePath">The normalized vault file path.</param>
        /// <param name="content">The patch content or payload.</param>
        /// <param name="operation">The patch operation mode (e.g., "replace", "insert", "remove").</param>
        /// <param name="ct">A cancellation token to cancel the operation.</param>
        /// <returns>An <see cref="OperationResult"/> describing the outcome.</returns>
        Task<OperationResult> PatchAsync(string filePath, string content, string operation, CancellationToken ct = default);

        /// <summary>
        /// Deletes the file at the specified path.
        /// </summary>
        /// <param name="filePath">The normalized vault file path.</param>
        /// <param name="ct">A cancellation token to cancel the operation.</param>
        /// <returns>An <see cref="OperationResult"/> describing the outcome.</returns>
        Task<OperationResult> DeleteAsync(string filePath, CancellationToken ct = default);

        /// <summary>
        /// Creates a new file with the given content at the specified path.
        /// </summary>
        /// <param name="filePath">The normalized vault file path.</param>
        /// <param name="content">The content to write to the new file.</param>
        /// <param name="ct">A cancellation token to cancel the operation.</param>
        /// <returns>An <see cref="OperationResult"/> describing the outcome.</returns>
        Task<OperationResult> CreateAsync(string filePath, string content, CancellationToken ct = default);
    }
}