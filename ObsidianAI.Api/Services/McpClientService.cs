using Microsoft.Extensions.Hosting;
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
        private readonly Lazy<Task<McpClient>> _lazyClient;
        private McpClient? _client;
        // No need for _isInitialized field since we rely on the lazy initialization

        public McpClientService()
        {
            _lazyClient = new Lazy<Task<McpClient>>(CreateClientAsync);
        }

        public McpClient Client
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
            // Initialize the client during startup
            _client = await _lazyClient.Value;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // McpClient doesn't implement IDisposable, so no disposal needed
            return Task.CompletedTask;
        }

        private async Task<McpClient> CreateClientAsync()
        {
            var options = new HttpClientTransportOptions
            {
                Endpoint = new Uri("http://localhost:8033/mcp")
            };
            var transport = new HttpClientTransport(options);
            return await McpClient.CreateAsync(transport);
        }
    }
}