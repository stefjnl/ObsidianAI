using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ObsidianAI.Application.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ObsidianAI.Api.Services
{
    /// <summary>
    /// Service to handle thread-safe lazy initialization of McpClient.
    /// Uses Lazy&lt;Task&lt;T&gt;&gt; pattern to ensure exactly one initialization across concurrent access.
    /// </summary>
    public class McpClientService : IHostedService, IMcpClientProvider
    {
        private readonly Lazy<Task<McpClient?>> _clientTask;
        private readonly ILogger<McpClientService> _logger;

        public McpClientService(ILogger<McpClientService> logger)
        {
            _logger = logger;
            
            // Lazy<T> with ExecutionAndPublication ensures thread-safe, single initialization
            // The factory will only execute once, even under concurrent access
            _clientTask = new Lazy<Task<McpClient?>>(
                () => CreateClientAsync(CancellationToken.None),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public Task<McpClient?> GetClientAsync(CancellationToken cancellationToken = default)
        {
            // Note: cancellationToken cannot be propagated to Lazy initialization
            // The first caller's initialization completes for all subsequent callers
            // This is by design - initialization happens once and is shared
            return _clientTask.Value;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Eagerly initialize the client during application startup
                // This allows us to log initialization success/failure early
                var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
                
                if (client != null)
                {
                    _logger.LogInformation("MCP client initialized successfully");
                }
                else
                {
                    _logger.LogWarning("MCP client initialization returned null - continuing without MCP functionality");
                }
            }
            catch (Exception ex)
            {
                // Even if initialization fails during startup, the application continues
                // Subsequent GetClientAsync calls will return the cached failure (null)
                _logger.LogError(ex, "Failed to initialize MCP client during startup - continuing without MCP functionality");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // McpClient doesn't implement IDisposable, so no disposal needed
            return Task.CompletedTask;
        }

        private async Task<McpClient?> CreateClientAsync(CancellationToken cancellationToken)
        {
            try
            {
                var mcpEndpoint = Environment.GetEnvironmentVariable("MCP_ENDPOINT");
                if (string.IsNullOrEmpty(mcpEndpoint))
                {
                    _logger.LogWarning("MCP_ENDPOINT environment variable not set. MCP functionality will be disabled.");
                    return null;
                }

                var options = new HttpClientTransportOptions
                {
                    Endpoint = new Uri(mcpEndpoint)
                };
                var transport = new HttpClientTransport(options);
                var client = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Successfully connected to MCP server at {McpEndpoint}", mcpEndpoint);
                return client;
            }
            catch (Exception ex)
            {
                var mcpEndpoint = Environment.GetEnvironmentVariable("MCP_ENDPOINT");
                _logger.LogWarning(ex, "Failed to connect to MCP server at {McpEndpoint}. MCP functionality will be disabled.", mcpEndpoint);
                return null;
            }
        }
    }
}