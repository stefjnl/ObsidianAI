namespace ObsidianAI.Infrastructure.Configuration;

public class NanoGptSettings
{
    public const string SectionName = "AIProviders:NanoGpt";
    
    // New architecture properties
    public string BaseUrl { get; set; } = "https://nano-gpt.com/api/v1";
    public string DefaultModel { get; set; } = "openai/gpt-oss-120b";
    public int TimeoutSeconds { get; set; } = 60;
    public int MaxRetries { get; set; } = 2;

    // Backward-compatible properties for existing agents/services
    // Map to new properties to keep a single source of truth
    public string Endpoint
    {
        get => BaseUrl;
        set => BaseUrl = value;
    }
    public string Model 
    {
        get => DefaultModel;
        set => DefaultModel = value;
    }

    // ApiKey is optionally bound from configuration; user secrets or environment can override
    public string? ApiKey { get; set; } = string.Empty;
}