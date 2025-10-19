namespace ObsidianAI.Infrastructure.LLM.Clients;

using ObsidianAI.Domain.Models;
using ObsidianAI.Domain.Ports;
using ObsidianAI.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

public class OpenRouterClient : IAIClient
{
    private readonly HttpClient _httpClient;
    private readonly OpenRouterSettings _settings;
    private readonly string _apiKey;
    private readonly ILogger<OpenRouterClient> _logger;

    public OpenRouterClient(
        HttpClient httpClient,
        IOptions<OpenRouterSettings> settings,
        IConfiguration configuration,
        ILogger<OpenRouterClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        
        _apiKey = configuration["OpenRouter:ApiKey"] 
            ?? throw new InvalidOperationException("OpenRouter API key not found. Please provide it via configuration (e.g., appsettings.json, environment variables, user secrets, or other supported providers) using the key 'OpenRouter:ApiKey'.");
    }

    public string ProviderName => "OpenRouter";

    public async Task<AIResponse> CallAsync(AIRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _apiKey);

            var model = string.IsNullOrEmpty(request.ModelName) 
                ? _settings.DefaultModel 
                : request.ModelName;

            var payload = new
            {
                model,
                messages = new[]
                {
                    new { role = "system", content = request.SystemMessage ?? "You are a helpful assistant." },
                    new { role = "user", content = request.Prompt }
                },
                temperature = request.Temperature,
                max_tokens = request.MaxTokens
            };

            var response = await _httpClient.PostAsJsonAsync(
                "chat/completions",
                payload,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            
            var content = result
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            var tokensUsed = result
                .GetProperty("usage")
                .GetProperty("total_tokens")
                .GetInt32();

            _logger.LogInformation(
                "OpenRouter call completed. Model: {Model}, Tokens: {Tokens}",
                model,
                tokensUsed);

            return new AIResponse
            {
                Content = content,
                Model = model,
                TokensUsed = tokensUsed,
                ProviderName = ProviderName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenRouter API call failed");
            throw;
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.GetAsync("models", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenRouter health check failed");
            return false;
        }
    }

    public async Task<IEnumerable<string>> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.GetAsync("models", cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            
            return result
                .GetProperty("data")
                .EnumerateArray()
                .Select(m => m.GetProperty("id").GetString() ?? string.Empty)
                .Where(id => !string.IsNullOrEmpty(id))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve OpenRouter models");
            return Enumerable.Empty<string>();
        }
    }
}