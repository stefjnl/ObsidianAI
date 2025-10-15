using Microsoft.Extensions.Diagnostics.HealthChecks;
using ModelContextProtocol.Client;
using ObsidianAI.Application.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ObsidianAI.Infrastructure.HealthChecks;

/// <summary>
/// Health check that verifies connectivity with the configured MCP gateway.
/// </summary>
public sealed class McpHealthCheck : IHealthCheck
{
    private readonly IMcpClientProvider _clientProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="McpHealthCheck"/> class.
    /// </summary>
    /// <param name="clientProvider">Provider used to obtain the shared MCP client instance.</param>
    public McpHealthCheck(IMcpClientProvider clientProvider)
    {
        _clientProvider = clientProvider ?? throw new ArgumentNullException(nameof(clientProvider));
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var client = await _clientProvider.GetClientAsync(cancellationToken).ConfigureAwait(false);
        if (client is null)
        {
            return HealthCheckResult.Degraded("MCP client is not initialized");
        }

        try
        {
            await client.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy("MCP gateway reachable");
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Healthy("MCP health check cancelled");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Failed to reach MCP gateway", ex);
        }
    }
}
