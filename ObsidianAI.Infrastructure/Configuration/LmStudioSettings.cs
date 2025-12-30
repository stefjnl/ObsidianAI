namespace ObsidianAI.Infrastructure.Configuration;

public class LMStudioSettings
{
    public const string SectionName = "AIProviders:LMStudio";
    
    // New architecture properties
    public string BaseUrl { get; set; } = "http://localhost:1234/v1";
    public string DefaultModel { get; set; } = "local-model";
    public int TimeoutSeconds { get; set; } = 45;
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

    // LM Studio often uses a placeholder/local token; keep overridable
    public string ApiKey { get; set; } = "lm-studio";
}