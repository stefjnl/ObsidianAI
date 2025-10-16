using System.Threading;
using System.Threading.Tasks;

namespace ObsidianAI.Application.Services;

/// <summary>
/// Resolves user-provided vault paths to the canonical paths returned by the Obsidian vault tooling.
/// </summary>
public interface IVaultPathResolver
{
    /// <summary>
    /// Attempts to resolve the provided path or label to a concrete vault path.
    /// Falls back to basic normalization when the MCP client is unavailable.
    /// </summary>
    /// <param name="candidatePath">Raw filename or folder description provided by the model or user.</param>
    /// <param name="cancellationToken">Token used to cancel the resolution.</param>
    /// <returns>A resolved vault path or an empty string when resolution fails.</returns>
    Task<string> ResolveAsync(string candidatePath, CancellationToken cancellationToken = default);
}
