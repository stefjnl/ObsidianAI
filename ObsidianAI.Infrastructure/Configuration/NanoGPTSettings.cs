namespace ObsidianAI.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for NanoGPT provider.
/// Bound via Options pattern from configuration section: LLM:NanoGPT.
/// </summary>
public class NanoGptSettings
{
    /// <summary>
    /// The endpoint URL for NanoGPT server.
    /// Example: http://localhost:8000
    /// </summary>
    public required string Endpoint { get; set; }

    /// <summary>
    /// The API key used to authenticate requests to NanoGPT (if required).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// The model name identifier.
    /// Example: nanogpt-shakespeare
    /// </summary>
    public required string Model { get; set; }
}
