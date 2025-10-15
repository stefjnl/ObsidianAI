using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ObsidianAI.Api.Services
{
    /// <summary>
    /// Service to handle async initialization of McpClient
    /// </summary>
    public class McpClientService : IHostedService
    {
        private readonly Lazy<Task<McpClient?>> _lazyClient;
        private McpClient? _client;
        private readonly ILogger<McpClientService> _logger;
        // No need for _isInitialized field since we rely on the lazy initialization

        public McpClientService(ILogger<McpClientService> logger)
        {
            _logger = logger;
            _lazyClient = new Lazy<Task<McpClient?>>(CreateClientAsync);
        }

        public McpClient? Client
        {
            get
            {
                if (_client == null)
                {
                    // Wait for the client to be created (only happens once)
                    _client = _lazyClient.Value.GetAwaiter().GetResult();
                }
                return _client;
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Initialize the client during startup
                _client = await _lazyClient.Value;
                if (_client != null)
                {
                    _logger.LogInformation("MCP client initialized successfully");
                }
                else
                {
                    _logger.LogWarning("MCP client initialization failed - continuing without MCP functionality");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize MCP client - continuing without MCP functionality");
                _client = null;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // McpClient doesn't implement IDisposable, so no disposal needed
            return Task.CompletedTask;
        }

        private async Task<McpClient?> CreateClientAsync()
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
                var client = await McpClient.CreateAsync(transport);
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