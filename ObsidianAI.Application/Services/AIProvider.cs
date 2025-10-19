namespace ObsidianAI.Application.Services;

using ObsidianAI.Application.Contracts;
using ObsidianAI.Domain.Models;
using ObsidianAI.Domain.Ports;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

/// <summary>
/// Application service for AI content generation.
/// Orchestrates provider selection, caching, retries, and business logic.
/// This is a CONCRETE class, not an interface.
/// </summary>
public class AIProvider
{
    private readonly IAIClientFactory _factory;
    private readonly IProviderSelectionStrategy _selectionStrategy;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AIProvider> _logger;
    private readonly AIProviderOptions _options;

    public AIProvider(
        IAIClientFactory factory,
        IProviderSelectionStrategy selectionStrategy,
        IMemoryCache cache,
        IOptions<AIProviderOptions> options,
        ILogger<AIProvider> logger)
    {
        _factory = factory;
        _selectionStrategy = selectionStrategy;
        _cache = cache;
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
        // 1. Validate input
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt cannot be empty", nameof(prompt));
        }

        // 2. Check cache
        var cacheKey = GenerateCacheKey(prompt, context, preferredProvider);
        if (_options.EnableCaching && _cache.TryGetValue(cacheKey, out object? cachedObj))
        {
            if (cachedObj is string cached && cached.Length > 0)
            {
                _logger.LogInformation("Cache hit for prompt hash: {Hash}", cacheKey.GetHashCode());
                return cached;
            }
        }

        // 3. Select provider
        var providerName = await _selectionStrategy.SelectProviderAsync(
            preferredProvider, 
            cancellationToken);
        
        var client = _factory.GetClient(providerName) 
            ?? throw new InvalidOperationException($"Provider {providerName} not available");

        // 4. Build request with business rules
        var request = BuildRequest(prompt, context, providerName, modelOverride);

        // 5. Execute with fallback
        AIResponse response;
        try
        {
            response = await client.CallAsync(request, cancellationToken);
            _logger.LogInformation(
                "Generated content using {Provider}, model: {Model}, tokens: {Tokens}", 
                providerName,
                response.Model,
                response.TokensUsed);
        }
        catch (Exception ex) when (_options.EnableFallback)
        {
            _logger.LogWarning(ex, "Primary provider {Provider} failed, trying fallback", providerName);
            
            var fallbackProvider = await _selectionStrategy.SelectProviderAsync(
                cancellationToken: cancellationToken);
            
            var fallbackClient = _factory.GetClient(fallbackProvider)
                ?? throw new InvalidOperationException("No fallback provider available");
            
            var fallbackRequest = BuildRequest(prompt, context, fallbackProvider, modelOverride);
            response = await fallbackClient.CallAsync(fallbackRequest, cancellationToken);
        }

        // 6. Post-process and cache
        var content = response.Content.Trim();
        
        if (_options.EnableCaching)
        {
            _cache.Set(
                cacheKey, 
                content, 
                TimeSpan.FromMinutes(_options.CacheDurationMinutes));
        }

        return content;
    }

    public async Task<bool> IsProviderAvailableAsync(
        string providerName, 
        CancellationToken cancellationToken = default)
    {
        var client = _factory.GetClient(providerName);
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
        return await _factory.GetModelsAsync(providerName, cancellationToken);
    }

    private AIRequest BuildRequest(
        string prompt, 
        string? context, 
        string providerName,
        string? modelOverride)
    {
        var model = modelOverride 
            ?? _options.ModelOverrides.GetValueOrDefault(providerName) 
            ?? string.Empty;

        return new AIRequest
        {
            Prompt = prompt,
            SystemMessage = context ?? _options.DefaultSystemMessage,
            ModelName = model,
            Temperature = _options.Temperature,
            MaxTokens = _options.MaxTokens
        };
    }

    private static string GenerateCacheKey(string prompt, string? context, string? provider)
    {
        return $"{provider ?? "default"}:{context?.GetHashCode() ?? 0}:{prompt.GetHashCode()}";
    }
}