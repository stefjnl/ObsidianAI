namespace ObsidianAI.Infrastructure.Configuration;

using System.Linq;

/// <summary>
/// Configuration for a single NanoGPT model.
/// </summary>
public class NanoGptModelConfig
{
    /// <summary>
    /// Display name for the model (e.g., "GLM 4.7").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Model identifier used in API calls (e.g., "zai-org/glm-4.7").
    /// </summary>
    public string Identifier { get; set; } = string.Empty;
}

/// <summary>
/// NanoGPT provider settings with multi-model support.
/// </summary>
public class NanoGptSettings
{
    public const string SectionName = "LLM:NanoGPT";

    /// <summary>
    /// Base URL for the NanoGPT API.
    /// </summary>
    public string BaseUrl { get; set; } = "https://nano-gpt.com/api/v1";

    /// <summary>
    /// Default model identifier to use when none specified.
    /// </summary>
    public string DefaultModel { get; set; } = "zai-org/glm-4.7";

    /// <summary>
    /// Available models for selection.
    /// </summary>
    public NanoGptModelConfig[] Models { get; set; } = [];

    public int TimeoutSeconds { get; set; } = 60;
    public int MaxRetries { get; set; } = 2;

    /// <summary>
    /// Backward-compatible endpoint property.
    /// </summary>
    public string Endpoint
    {
        get => BaseUrl;
        set => BaseUrl = value;
    }

    /// <summary>
    /// Backward-compatible model property.
    /// </summary>
    public string Model
    {
        get => DefaultModel;
        set => DefaultModel = value;
    }

    /// <summary>
    /// API key for NanoGPT. Can be set via user secrets or environment variables.
    /// </summary>
    public string? ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets the model identifier by its display name.
    /// </summary>
    public string? GetModelIdentifierByName(string name)
    {
        return Models.FirstOrDefault(m => m.Name == name)?.Identifier;
    }

    /// <summary>
    /// Gets all available model display names.
    /// </summary>
    public IEnumerable<string> GetAvailableModelNames()
    {
        return Models.Select(m => m.Name);
    }

    /// <summary>
    /// Checks if a model identifier is valid.
    /// </summary>
    public bool IsValidModelIdentifier(string identifier)
    {
        return Models.Any(m => m.Identifier == identifier);
    }
}