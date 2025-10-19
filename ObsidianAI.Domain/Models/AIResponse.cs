namespace ObsidianAI.Domain.Models;

/// <summary>
/// Provider-agnostic AI response
/// </summary>
public class AIResponse
{
    public string Content { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}