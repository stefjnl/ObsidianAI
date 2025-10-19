using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ObsidianAI.Application.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ObsidianAI.Infrastructure.Services;

/// <summary>
/// Hosted service that initializes MCP clients on application startup.
/// Improves first-request performance and enables health checks to work immediately.
/// </summary>
public sealed class McpClientWarmupService : IHostedService
{
    private readonly IMcpClientProvider _clientProvider;
    private readonly ILogger<McpClientWarmupService> _logger;

    public McpClientWarmupService(
        IMcpClientProvider clientProvider,
        ILogger<McpClientWarmupService> logger)
    {
        _clientProvider = clientProvider ?? throw new ArgumentNullException(nameof(clientProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting MCP client warmup");

        var servers = _clientProvider.GetAvailableServers();
        if (servers.Count == 0)
        {
            _logger.LogWarning("No MCP servers configured for warmup");
            return;
        }

        var warmupTasks = servers.Select(async serverName =>
        {
            try
            {
                var client = await _clientProvider.GetClientAsync(serverName, cancellationToken);
                if (client != null)
                {
                    _logger.LogInformation("Warmed up MCP client: {ServerName}", serverName);
                }
                else
                {
                    _logger.LogWarning("Failed to warm up MCP client: {ServerName}", serverName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during warmup of {ServerName}", serverName);
            }
        });

        await Task.WhenAll(warmupTasks);
        _logger.LogInformation("MCP client warmup completed");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MCP client warmup service stopping");
        return Task.CompletedTask;
    }
}
