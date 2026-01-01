namespace ObsidianAI.Application.Services;

using ObsidianAI.Application.Contracts;
using ObsidianAI.Domain.Ports;
using Microsoft.Extensions.Logging;

/// <summary>
/// Provider selection strategy - NanoGPT is the sole provider.
/// </summary>
public class HealthBasedProviderSelection : IProviderSelectionStrategy
{
    private readonly IAIClientFactory _factory;
    private readonly ILogger<HealthBasedProviderSelection> _logger;

    public HealthBasedProviderSelection(
        IAIClientFactory factory,
        ILogger<HealthBasedProviderSelection> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<string> SelectProviderAsync(
        string? userPreference = null,
        CancellationToken cancellationToken = default)
    {
        // NanoGPT is the sole provider
        var client = _factory.GetClient("NanoGPT");
        if (client != null && await client.IsHealthyAsync(cancellationToken))
        {
            _logger.LogInformation("Using NanoGPT provider");
            return "NanoGPT";
        }

        throw new InvalidOperationException("NanoGPT provider is not available");
    }
}