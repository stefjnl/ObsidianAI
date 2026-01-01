namespace ObsidianAI.Domain.Ports;

using System.Collections.Generic;

/// <summary>
/// Model information for runtime selection.
/// </summary>
public record ModelInfo(string Name, string Identifier);

/// <summary>
/// Provides runtime access to the active LLM model selection and supports switching models without restarts.
/// </summary>
public interface ILlmProviderRuntimeStore
{
    /// <summary>
    /// Gets the provider name (always "NanoGPT").
    /// </summary>
    string CurrentProvider { get; }

    /// <summary>
    /// Gets the current model identifier.
    /// </summary>
    string CurrentModel { get; }

    /// <summary>
    /// Gets the available models for selection.
    /// </summary>
    IReadOnlyList<ModelInfo> GetAvailableModels();

    /// <summary>
    /// Attempts to switch the active model at runtime.
    /// </summary>
    /// <param name="modelIdentifier">The model identifier to switch to.</param>
    /// <param name="error">When unsuccessful, receives a failure description.</param>
    /// <returns><c>true</c> when the model was switched; otherwise <c>false</c>.</returns>
    bool TrySwitchModel(string modelIdentifier, out string? error);

    /// <summary>
    /// Legacy method for backward compatibility. Always returns true since NanoGPT is the only provider.
    /// </summary>
    bool TrySwitchProvider(string providerName, out string model, out string? error);
}
