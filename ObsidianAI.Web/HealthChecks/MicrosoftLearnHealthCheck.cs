using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ObsidianAI.Application.Services;

namespace ObsidianAI.Web.HealthChecks;

/// <summary>
/// Health check that verifies connectivity with the Microsoft Learn MCP endpoint.
/// </summary>
public sealed class MicrosoftLearnHealthCheck : IHealthCheck
{
    private readonly IMicrosoftLearnMcpClientProvider _client;

    public MicrosoftLearnHealthCheck(IMicrosoftLearnMcpClientProvider client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var client = await _client.GetClientAsync(cancellationToken).ConfigureAwait(false);
        if (client is null)
        {
            return HealthCheckResult.Degraded("Microsoft Learn MCP endpoint unavailable or not configured.");
        }

        try
        {
            await client.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy("Microsoft Learn MCP reachable.");
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Healthy("Microsoft Learn MCP health check cancelled.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Failed to reach Microsoft Learn MCP.", ex);
        }
    }
}
