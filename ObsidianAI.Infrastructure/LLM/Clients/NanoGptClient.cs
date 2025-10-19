namespace ObsidianAI.Infrastructure.LLM.Clients;

using ObsidianAI.Domain.Models;
using ObsidianAI.Domain.Ports;
using ObsidianAI.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class NanoGptClient : IAIClient
{
    private readonly HttpClient _httpClient;
    private readonly NanoGptSettings _settings;
    private readonly string _apiKey;
    private readonly ILogger<NanoGptClient> _logger;

    public NanoGptClient(
        HttpClient httpClient,
        IOptions<NanoGptSettings> settings,
        IConfiguration configuration,
        ILogger<NanoGptClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        
        _apiKey = configuration["NanoGpt:ApiKey"] 
            ?? throw new InvalidOperationException("NanoGpt API key not found in user secrets");
    }

    public string ProviderName => "NanoGpt";

    public async Task<AIResponse> CallAsync(AIRequest request, CancellationToken cancellationToken = default)
    {
        // TODO: Implement NanoGpt-specific API call
        // This is a placeholder - adjust based on actual NanoGpt API format
        await Task.Yield();
        throw new NotImplementedException("Implement NanoGpt API call based on provider documentation");
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "NanoGpt health check failed");
            return false;
        }
    }

    public async Task<IEnumerable<string>> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement based on NanoGpt API
        await Task.Yield();
        throw new NotImplementedException("Implement NanoGpt model listing");
    }
}