namespace ObsidianAI.Application.Services;

using ObsidianAI.Application.Contracts;
using ObsidianAI.Domain.Models;
using ObsidianAI.Domain.Ports;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

/// <summary>
/// Application service for AI content generation.
/// NanoGPT is the sole provider - simplified orchestration.
/// </summary>
public class AIProvider
{
    private readonly IAIClientFactory _factory;
    private readonly ILogger<AIProvider> _logger;
    private readonly AIProviderOptions _options;

    public AIProvider(
        IAIClientFactory factory,
        IOptions<AIProviderOptions> options,
        ILogger<AIProvider> logger)
    {
        _factory = factory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GenerateContentAsync(
        string prompt,
        string? context = null,
        string? preferredProvider = null,
        string? modelOverride = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt cannot be empty", nameof(prompt));
        }

        // NanoGPT is the sole provider
        var client = _factory.GetClient("NanoGPT")
            ?? throw new InvalidOperationException("NanoGPT provider not available");

        var request = BuildRequest(prompt, context, modelOverride);

        var response = await client.CallAsync(request, cancellationToken);
        _logger.LogInformation(
            "Generated content using NanoGPT, model: {Model}, tokens: {Tokens}",
            response.Model,
            response.TokensUsed);

        return response.Content.Trim();
    }

    public async Task<bool> IsProviderAvailableAsync(
        string providerName,
        CancellationToken cancellationToken = default)
    {
        var client = _factory.GetClient("NanoGPT");
        if (client == null) return false;

        return await client.IsHealthyAsync(cancellationToken);
    }

    public async Task<IEnumerable<string>> GetAvailableProvidersAsync(
        CancellationToken cancellationToken = default)
    {
        var providers = new List<string>();

        foreach (var client in _factory.GetAllClients())
        {
            if (await client.IsHealthyAsync(cancellationToken))
            {
                providers.Add(client.ProviderName);
            }
        }

        return providers;
    }

    public async Task<IEnumerable<string>> GetModelsForProviderAsync(
        string providerName,
        CancellationToken cancellationToken = default)
    {
        return await _factory.GetModelsAsync("NanoGPT", cancellationToken);
    }

    private AIRequest BuildRequest(string prompt, string? context, string? modelOverride)
    {
        var model = modelOverride ?? _options.DefaultModel;

        return new AIRequest
        {
            Prompt = prompt,
            SystemMessage = context ?? "You are a helpful AI assistant.",
            ModelName = model,
            Temperature = _options.Temperature,
            MaxTokens = _options.MaxTokens
        };
    }
}