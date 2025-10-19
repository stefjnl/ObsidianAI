using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ObsidianAI.Application.Services;

namespace ObsidianAI.Web.Services;

public sealed class MicrosoftLearnMcpClient : IMicrosoftLearnMcpClientProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MicrosoftLearnMcpClient> _logger;
    private readonly SemaphoreSlim _sync = new(1, 1);
    private McpClient? _client;

    public MicrosoftLearnMcpClient(IConfiguration configuration, ILogger<MicrosoftLearnMcpClient> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<McpClient?> GetClientAsync(CancellationToken cancellationToken = default)
    {
        if (_client != null)
        {
            return _client;
        }

        await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_client != null)
            {
                return _client;
            }

            var endpoint = _configuration["MicrosoftLearnMcp:Endpoint"]
                ?? Environment.GetEnvironmentVariable("MICROSOFT_LEARN_MCP_ENDPOINT");

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                _logger.LogDebug("Microsoft Learn MCP endpoint not configured.");
                return null;
            }

            try
            {
                var transport = new HttpClientTransport(new HttpClientTransportOptions { Endpoint = new Uri(endpoint) });
                _client = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Connected to Microsoft Learn MCP at {Endpoint}", endpoint);
                return _client;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to Microsoft Learn MCP at {Endpoint}", endpoint);
                _client = null;
                return null;
            }
        }
        finally
        {
            _sync.Release();
        }
    }
}
