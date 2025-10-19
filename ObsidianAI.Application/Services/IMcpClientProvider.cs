using ModelContextProtocol.Client;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ObsidianAI.Application.Services;

/// <summary>
/// Provides unified access to multiple MCP server clients.
/// </summary>
public interface IMcpClientProvider
{
    /// <summary>
    /// Gets or creates an MCP client for the specified server.
    /// Returns null if server is not configured or connection fails.
    /// </summary>
    Task<McpClient?> GetClientAsync(string serverName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lists all available tools from the specified server.
    /// </summary>
    Task<IEnumerable<object>> ListToolsAsync(string serverName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes a tool on the specified server.
    /// </summary>
    Task<object> CallToolAsync(
        string serverName, 
        string toolName, 
        Dictionary<string, object?> arguments,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns the list of configured and available server names.
    /// </summary>
    IReadOnlyCollection<string> GetAvailableServers();
}
