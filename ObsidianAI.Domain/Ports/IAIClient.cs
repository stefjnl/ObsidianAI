using ObsidianAI.Domain.Models;

namespace ObsidianAI.Domain.Ports;

/// <summary>
/// Low-level infrastructure interface for AI provider clients.
/// Handles provider-specific HTTP communication, auth, and protocol details.
/// </summary>
public interface IAIClient
{
    /// <summary>
    /// Provider identifier (e.g., "OpenRouter", "NanoGpt", "LMStudio")
    /// </summary>
    string ProviderName { get; }
    
    /// <summary>
    /// Execute AI request against the provider's API
    /// </summary>
    Task<AIResponse> CallAsync(AIRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if provider is available and responsive
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get list of available models from this provider
    /// </summary>
    Task<IEnumerable<string>> GetModelsAsync(CancellationToken cancellationToken = default);
}