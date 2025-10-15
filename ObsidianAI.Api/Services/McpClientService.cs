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
    /// Service to handle async initialization of McpClient
    /// </summary>
    public class McpClientService : IHostedService, IMcpClientProvider
    {
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private McpClient? _client;
        private readonly ILogger<McpClientService> _logger;
        private bool _initialized;

        public McpClientService(ILogger<McpClientService> logger)
        {
            _logger = logger;
        }

        public async Task<McpClient?> GetClientAsync(CancellationToken cancellationToken = default)
        {
            if (_initialized)
            {
                return _client;
            }

            await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_initialized)
                {
                    return _client;
                }

                _client = await CreateClientAsync(cancellationToken).ConfigureAwait(false);
                _initialized = true;
                return _client;
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
                if (client != null)
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