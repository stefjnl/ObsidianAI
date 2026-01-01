namespace ObsidianAI.Infrastructure.Configuration;

/// <summary>
/// LLM settings - NanoGPT is the sole provider.
/// </summary>
public class LlmSettings
{
    /// <summary>
    /// The LLM provider (always "NanoGPT").
    /// </summary>
    public string Provider { get; set; } = "NanoGPT";

    /// <summary>
    /// NanoGPT configuration with multi-model support.
    /// </summary>
    public NanoGptSettings NanoGPT { get; set; } = new();
}