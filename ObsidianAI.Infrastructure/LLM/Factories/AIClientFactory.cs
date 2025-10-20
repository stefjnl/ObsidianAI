namespace ObsidianAI.Infrastructure.LLM.Factories;

using ObsidianAI.Domain.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

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
        // Use service locator pattern to resolve concrete agent by provider name
        var client = providerName switch
        {
            "OpenRouter" => _serviceProvider.GetRequiredService<OpenRouterChatAgent>() as IAIClient,
            "LMStudio" => _serviceProvider.GetRequiredService<LmStudioChatAgent>() as IAIClient,
            "NanoGPT" => _serviceProvider.GetRequiredService<NanoGptChatAgent>() as IAIClient,
            _ => null
        };
        
        if (client == null)
        {
            _logger.LogWarning("Provider {Provider} not found", providerName);
        }
        
        return client;
    }

    public IEnumerable<IAIClient> GetAllClients()
    {
        // Return all registered agent-backed clients
        yield return _serviceProvider.GetRequiredService<OpenRouterChatAgent>();
        yield return _serviceProvider.GetRequiredService<LmStudioChatAgent>();
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
            _logger.LogError(ex, "Failed to get models for {Provider}", providerName);
            return Enumerable.Empty<string>();
        }
    }
}