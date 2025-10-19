namespace ObsidianAI.Infrastructure.Configuration;

/// <summary>
/// LLM settings containing provider and specific provider configurations.
/// </summary>
public class LlmSettings
{
    /// <summary>
    /// The LLM provider to use (e.g., "LMStudio" or "OpenRouter").
    /// </summary>
    public required string Provider { get; set; }

    /// <summary>
    /// LMStudio-specific configuration.
    /// </summary>
    public required LMStudioSettings LMStudio { get; set; }

    /// <summary>
    /// OpenRouter-specific configuration.
    /// </summary>
    public required OpenRouterSettings OpenRouter { get; set; }

    /// <summary>
    /// NanoGPT-specific configuration.
    /// </summary>
    public NanoGptSettings? NanoGPT { get; set; }
}