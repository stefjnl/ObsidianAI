using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Client;

namespace ObsidianAI.Application.Services;

/// <summary>
/// Provides access to the Microsoft Learn Model Context Protocol client.
/// </summary>
public interface IMicrosoftLearnMcpClientProvider
{
    /// <summary>
    /// Retrieves the Microsoft Learn MCP client, if available.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the initialization.</param>
    /// <returns>The initialized client, or <c>null</c> when unavailable.</returns>
    Task<McpClient?> GetClientAsync(CancellationToken cancellationToken = default);
}
