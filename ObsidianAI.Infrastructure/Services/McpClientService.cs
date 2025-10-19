using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ObsidianAI.Application.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ObsidianAI.Infrastructure.Services;

public sealed class McpClientService : IMcpClientProvider, IAsyncDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<McpClientService> _logger;
    private readonly Dictionary<string, McpClient?> _clients = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    public McpClientService(
        IConfiguration configuration,
        ILogger<McpClientService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<McpClient?> GetClientAsync(string serverName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serverName))
            throw new ArgumentException("Server name cannot be empty", nameof(serverName));

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Return cached client if available
            if (_clients.TryGetValue(serverName, out var existingClient))
            {
                _logger.LogDebug("Returning cached client for {ServerName}", serverName);
                return existingClient;
            }

            // Initialize new client
            var client = await InitializeClientAsync(serverName, cancellationToken);
            _clients[serverName] = client;
            return client;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<McpClient?> InitializeClientAsync(string serverName, CancellationToken cancellationToken)
    {
        try
        {
            var endpoint = GetEndpointForServer(serverName);
            if (string.IsNullOrEmpty(endpoint))
            {
                _logger.LogWarning("No endpoint configured for MCP server: {ServerName}", serverName);
                return null;
            }

            _logger.LogInformation("Initializing MCP client for {ServerName} at {Endpoint}", 
                serverName, endpoint);

            var transport = new HttpClientTransport(
                new HttpClientTransportOptions 
                { 
                    Endpoint = new Uri(endpoint) 
                });

            var client = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
            
            _logger.LogInformation("Successfully connected to MCP server: {ServerName}", serverName);
            return client;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MCP client for {ServerName}", serverName);
            return null;
        }
    }

    private string? GetEndpointForServer(string serverName)
    {
        // Try environment variable first (backward compatibility)
        // Format: OBSIDIAN_MCP_ENDPOINT, MICROSOFT_LEARN_MCP_ENDPOINT, etc.
        var envKey = $"{serverName.ToUpperInvariant().Replace("-", "_")}_MCP_ENDPOINT";
        var envEndpoint = _configuration[envKey];
        if (!string.IsNullOrEmpty(envEndpoint))
        {
            _logger.LogDebug("Using environment variable {EnvKey} for {ServerName}", envKey, serverName);
            return envEndpoint;
        }

        // Try structured configuration
        var configEndpoint = _configuration[$"McpServers:{serverName}:Endpoint"];
        if (!string.IsNullOrEmpty(configEndpoint))
        {
            _logger.LogDebug("Using configuration McpServers:{ServerName}:Endpoint", serverName);
            return configEndpoint;
        }

        return null;
    }

    public async Task<IEnumerable<object>> ListToolsAsync(string serverName, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(serverName, cancellationToken);
        if (client == null)
        {
            _logger.LogWarning("Cannot list tools: client not available for {ServerName}", serverName);
            return Enumerable.Empty<object>();
        }

        try
        {
            var result = await client.ListToolsAsync(cancellationToken: cancellationToken);
            _logger.LogInformation("Listed {Count} tools from {ServerName}", result.Count(), serverName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list tools from {ServerName}", serverName);
            return Enumerable.Empty<object>();
        }
    }

    public async Task<object> CallToolAsync(
        string serverName, 
        string toolName, 
        Dictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(serverName, cancellationToken);
        if (client == null)
        {
            throw new InvalidOperationException($"MCP client not available for server: {serverName}");
        }

        try
        {
            _logger.LogInformation("Calling tool {ToolName} on {ServerName}", toolName, serverName);
            return await client.CallToolAsync(toolName, arguments, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call tool {ToolName} on {ServerName}", toolName, serverName);
            throw;
        }
    }

    public IReadOnlyCollection<string> GetAvailableServers()
    {
        var section = _configuration.GetSection("McpServers");
        if (!section.Exists())
        {
            _logger.LogWarning("No McpServers configuration section found");
            return Array.Empty<string>();
        }

        var servers = section.GetChildren()
            .Where(s => s.GetValue<bool>("Enabled", defaultValue: true))
            .Select(s => s.Key)
            .ToList();

        _logger.LogDebug("Found {Count} available MCP servers: {Servers}", 
            servers.Count, string.Join(", ", servers));

        return servers.AsReadOnly();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        await _semaphore.WaitAsync();
        try
        {
            _logger.LogInformation("Disposing MCP clients for {Count} servers", _clients.Count);

            foreach (var kvp in _clients)
            {
                if (kvp.Value != null)
                {
                    try
                    {
                        await kvp.Value.DisposeAsync();
                        _logger.LogDebug("Disposed client for {ServerName}", kvp.Key);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error disposing client for {ServerName}", kvp.Key);
                    }
                }
            }

            _clients.Clear();
            _disposed = true;
        }
        finally
        {
            _semaphore.Release();
            _semaphore.Dispose();
        }
    }
}
