namespace ObsidianAI.Infrastructure.LLM.Factories;

using ObsidianAI.Domain.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Factory for AI clients - NanoGPT is the sole provider.
/// </summary>
public class AIClientFactory : IAIClientFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AIClientFactory> _logger;

    public AIClientFactory(
        IServiceProvider serviceProvider,
        ILogger<AIClientFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public IAIClient? GetClient(string providerName)
    {
        // NanoGPT is the sole provider - all requests go to NanoGPT
        if (!string.Equals(providerName, "NanoGPT", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Provider {Provider} requested but NanoGPT is the only provider. Using NanoGPT.", providerName);
        }

        return _serviceProvider.GetRequiredService<NanoGptChatAgent>() as IAIClient;
    }

    public IEnumerable<IAIClient> GetAllClients()
    {
        yield return _serviceProvider.GetRequiredService<NanoGptChatAgent>();
    }

    public async Task<IEnumerable<string>> GetModelsAsync(
        string providerName,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient(providerName);
        if (client == null)
        {
            return Enumerable.Empty<string>();
        }

        try
        {
            return await client.GetModelsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get models for NanoGPT");
            return Enumerable.Empty<string>();
        }
    }
}