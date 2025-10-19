namespace ObsidianAI.Application.Contracts;

/// <summary>
/// Application-level orchestration settings for AI provider usage
/// </summary>
public class AIProviderOptions
{
    public const string SectionName = "AIProviderOptions";
    
    public string DefaultProvider { get; set; } = "OpenRouter";
    public string FallbackProvider { get; set; } = "OpenRouter";
    public bool EnableFallback { get; set; } = true;
    public bool EnableCaching { get; set; } = true;
    public int CacheDurationMinutes { get; set; } = 10;
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 1000;
    public string DefaultSystemMessage { get; set; } = "You are a helpful AI assistant.";
    public Dictionary<string, string> ModelOverrides { get; set; } = new();
}