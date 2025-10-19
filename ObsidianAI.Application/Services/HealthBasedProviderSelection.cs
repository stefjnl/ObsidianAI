namespace ObsidianAI.Application.Services;

using ObsidianAI.Application.Contracts;
using ObsidianAI.Domain.Ports;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

public class HealthBasedProviderSelection : IProviderSelectionStrategy
{
    private readonly IAIClientFactory _factory;
    private readonly AIProviderOptions _options;
    private readonly ILogger<HealthBasedProviderSelection> _logger;

    public HealthBasedProviderSelection(
        IAIClientFactory factory,
        IOptions<AIProviderOptions> options,
        ILogger<HealthBasedProviderSelection> logger)
    {
        _factory = factory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> SelectProviderAsync(
        string? userPreference = null, 
        CancellationToken cancellationToken = default)
    {
        // 1. Try user preference if specified
        if (!string.IsNullOrWhiteSpace(userPreference))
        {
            var preferred = _factory.GetClient(userPreference);
            if (preferred != null && await preferred.IsHealthyAsync(cancellationToken))
            {
                _logger.LogInformation("Using user-preferred provider: {Provider}", userPreference);
                return userPreference;
            }
            _logger.LogWarning("Preferred provider {Provider} unavailable", userPreference);
        }

        // 2. Try default provider
        var defaultClient = _factory.GetClient(_options.DefaultProvider);
        if (defaultClient != null && await defaultClient.IsHealthyAsync(cancellationToken))
        {
            _logger.LogInformation("Using default provider: {Provider}", _options.DefaultProvider);
            return _options.DefaultProvider;
        }

        // 3. Try fallback provider
        var fallbackClient = _factory.GetClient(_options.FallbackProvider);
        if (fallbackClient != null && await fallbackClient.IsHealthyAsync(cancellationToken))
        {
            _logger.LogInformation("Using fallback provider: {Provider}", _options.FallbackProvider);
            return _options.FallbackProvider;
        }

        // 4. Find any healthy provider
        foreach (var client in _factory.GetAllClients())
        {
            if (await client.IsHealthyAsync(cancellationToken))
            {
                _logger.LogInformation("Using first available provider: {Provider}", client.ProviderName);
                return client.ProviderName;
            }
        }

        throw new InvalidOperationException("No healthy AI providers available");
    }
}