namespace ObsidianAI.Infrastructure.LLM.Factories;

using ObsidianAI.Domain.Ports;
using Microsoft.Extensions.Logging;

public class AIClientFactory : IAIClientFactory
{
    private readonly IEnumerable<IAIClient> _clients;
    private readonly ILogger<AIClientFactory> _logger;

    public AIClientFactory(
        IEnumerable<IAIClient> clients,
        ILogger<AIClientFactory> logger)
    {
        _clients = clients;
        _logger = logger;
    }

    public IAIClient? GetClient(string providerName)
    {
        var client = _clients.FirstOrDefault(c => 
            c.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));
        
        if (client == null)
        {
            _logger.LogWarning("Provider {Provider} not found", providerName);
        }
        
        return client;
    }

    public IEnumerable<IAIClient> GetAllClients() => _clients;

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