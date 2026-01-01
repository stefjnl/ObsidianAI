namespace ObsidianAI.Application.Contracts;

/// <summary>
/// Application-level orchestration settings for AI provider usage.
/// NanoGPT is the sole provider; only model selection varies.
/// </summary>
public class AIProviderOptions
{
    public const string SectionName = "AIProviderOptions";

    /// <summary>
    /// Default provider (always "NanoGPT").
    /// </summary>
    public string DefaultProvider { get; set; } = "NanoGPT";

    /// <summary>
    /// Default model identifier to use.
    /// </summary>
    public string DefaultModel { get; set; } = "zai-org/glm-4.7";

    /// <summary>
    /// Whether fallback is enabled (disabled since NanoGPT is sole provider).
    /// </summary>
    public bool EnableFallback { get; set; } = false;

    /// <summary>
    /// Temperature for LLM responses.
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Maximum tokens for LLM responses.
    /// </summary>
    public int MaxTokens { get; set; } = 1000;
}