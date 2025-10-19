namespace ObsidianAI.Domain.Ports;

/// <summary>
/// Provides runtime access to the active LLM provider selection and supports switching providers without restarts.
/// </summary>
public interface ILlmProviderRuntimeStore
{
    /// <summary>
    /// Gets the canonical provider name currently active for chat operations.
    /// </summary>
    string CurrentProvider { get; }

    /// <summary>
    /// Gets the model identifier associated with the current provider.
    /// </summary>
    string CurrentModel { get; }

    /// <summary>
    /// Attempts to switch the active provider at runtime.
    /// </summary>
    /// <param name="providerName">The provider name requested by the caller.</param>
    /// <param name="model">When successful, receives the resolved model identifier for the provider.</param>
    /// <param name="error">When unsuccessful, receives a failure description.</param>
    /// <returns><c>true</c> when the provider was switched; otherwise <c>false</c>.</returns>
    bool TrySwitchProvider(string providerName, out string model, out string? error);
}
