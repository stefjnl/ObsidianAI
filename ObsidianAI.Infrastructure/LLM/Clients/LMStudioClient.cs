namespace ObsidianAI.Infrastructure.LLM.Clients;

using ObsidianAI.Domain.Models;
using ObsidianAI.Domain.Ports;
using ObsidianAI.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

public class LMStudioClient : IAIClient
{
    private readonly HttpClient _httpClient;
    private readonly LMStudioSettings _settings;
    private readonly ILogger<LMStudioClient> _logger;

    public LMStudioClient(
        HttpClient httpClient,
        IOptions<LMStudioSettings> settings,
        ILogger<LMStudioClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        
        // Note: LMStudio typically doesn't require API key for local instances
    }

    public string ProviderName => "LMStudio";

    public async Task<AIResponse> CallAsync(AIRequest request, CancellationToken cancellationToken = default)
    {
        // TODO: Implement LMStudio-specific API call (OpenAI-compatible format)
        await Task.Yield();
        throw new NotImplementedException("Implement LMStudio API call (OpenAI-compatible)");
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("models", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LMStudio health check failed");
            return false;
        }
    }

    public async Task<IEnumerable<string>> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement based on LMStudio API (similar to OpenAI format)
        await Task.Yield();
        throw new NotImplementedException("Implement LMStudio model listing");
    }
}