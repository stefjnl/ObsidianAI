using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using ObsidianAI.Application.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ObsidianAI.Infrastructure.HealthChecks;

/// <summary>
/// Health check for all configured MCP servers.
/// Returns Healthy if all servers respond, Degraded if some fail, Unhealthy if all fail.
/// </summary>
public sealed class McpHealthCheck : IHealthCheck
{
    private readonly IMcpClientProvider _clientProvider;
    private readonly ILogger<McpHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="McpHealthCheck"/> class.
    /// </summary>
    /// <param name="clientProvider">Provider used to obtain MCP client instances.</param>
    /// <param name="logger">Logger for health check operations.</param>
    public McpHealthCheck(
        IMcpClientProvider clientProvider,
        ILogger<McpHealthCheck> logger)
    {
        _clientProvider = clientProvider ?? throw new ArgumentNullException(nameof(clientProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();
        var servers = _clientProvider.GetAvailableServers();

        if (servers.Count == 0)
        {
            _logger.LogWarning("No MCP servers configured");
            return HealthCheckResult.Degraded("No MCP servers configured", data: data);
        }

        var failures = new List<string>();
        var successes = new List<string>();

        foreach (var serverName in servers)
        {
            try
            {
                var tools = await _clientProvider.ListToolsAsync(serverName, cancellationToken);
                var toolCount = tools.Count();
                
                successes.Add(serverName);
                data[serverName] = $"✓ {toolCount} tools";
                
                _logger.LogDebug("Health check passed for {ServerName}: {ToolCount} tools", 
                    serverName, toolCount);
            }
            catch (Exception ex)
            {
                failures.Add(serverName);
                data[serverName] = $"✗ {ex.Message}";
                
                _logger.LogWarning(ex, "Health check failed for {ServerName}", serverName);
            }
        }

        data["summary"] = $"{successes.Count}/{servers.Count} servers healthy";

        return failures.Count == 0
            ? HealthCheckResult.Healthy($"All {servers.Count} MCP servers connected", data)
            : failures.Count == servers.Count
                ? HealthCheckResult.Unhealthy("All MCP servers failed", data: data)
                : HealthCheckResult.Degraded($"{failures.Count}/{servers.Count} servers failed: {string.Join(", ", failures)}", data: data);
    }
}
