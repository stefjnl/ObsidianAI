namespace ObsidianAI.Domain.Ports;

/// <summary>
/// Factory for resolving AI provider clients by name
/// </summary>
public interface IAIClientFactory
{
    /// <summary>
    /// Get client for specific provider, returns null if not found
    /// </summary>
    IAIClient? GetClient(string providerName);
    
    /// <summary>
    /// Get all registered clients
    /// </summary>
    IEnumerable<IAIClient> GetAllClients();
    
    /// <summary>
    /// Get available models for a specific provider
    /// </summary>
    Task<IEnumerable<string>> GetModelsAsync(string providerName, CancellationToken cancellationToken = default);
}