namespace ObsidianAI.Domain.Ports;

/// <summary>
/// Strategy for selecting which AI provider to use
/// </summary>
public interface IProviderSelectionStrategy
{
    /// <summary>
    /// Select provider based on user preference and availability
    /// </summary>
    Task<string> SelectProviderAsync(string? userPreference = null, CancellationToken cancellationToken = default);
}