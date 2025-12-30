namespace ObsidianAI.Infrastructure.Configuration;

public class OpenRouterSettings
{
    public const string SectionName = "AIProviders:OpenRouter";
    
    // New architecture properties
    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";
    public string DefaultModel { get; set; } = "google/gemini-2.5-flash-lite-preview-09-2025";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;

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