namespace ObsidianAI.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for OpenRouter provider.
/// Bound via Options pattern from configuration section: LLM:OpenRouter.
/// </summary>
public class OpenRouterSettings
{
    /// <summary>
    /// The endpoint URL for OpenRouter (OpenAI-compatible).
    /// Example: https://openrouter.ai/api/v1
    /// </summary>
    public required string Endpoint { get; set; }

    /// <summary>
    /// The API key used to authenticate requests to OpenRouter.
    /// </summary>
    public required string ApiKey { get; set; }

    /// <summary>
    /// The default model name to use for chat completions.
    /// Example: google/gemini-2.5-flash-lite-preview-09-2025
    /// </summary>
    public required string Model { get; set; }
}