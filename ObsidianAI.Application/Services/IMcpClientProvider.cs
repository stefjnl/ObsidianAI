using ModelContextProtocol.Client;
using System.Threading;
using System.Threading.Tasks;

namespace ObsidianAI.Application.Services;

/// <summary>
/// Provides access to a lazily initialized <see cref="McpClient"/> instance.
/// </summary>
public interface IMcpClientProvider
{
    /// <summary>
    /// Retrieves the shared <see cref="McpClient"/> instance, initializing it if necessary.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the initialization.</param>
    /// <returns>The initialized client, or <c>null</c> when MCP is unavailable.</returns>
    Task<McpClient?> GetClientAsync(CancellationToken cancellationToken = default);
}
