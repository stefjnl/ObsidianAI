namespace ObsidianAI.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for LM Studio provider.
/// Bound via Options pattern from configuration section: LLM:LMStudio.
/// </summary>
public class LmStudioSettings
{
    /// <summary>
    /// The endpoint URL for LMStudio (OpenAI-compatible).
    /// Example: http://localhost:1234/v1
    /// </summary>
    public required string Endpoint { get; set; }

    /// <summary>
    /// The API key used to authenticate requests to LMStudio.
    /// </summary>
    public required string ApiKey { get; set; }

    /// <summary>
    /// The default model name to use for chat completions.
    /// Example: openai/gpt-oss-20b
    /// </summary>
    public required string Model { get; set; }
}