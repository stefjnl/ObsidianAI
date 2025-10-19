namespace ObsidianAI.Domain.Models;

/// <summary>
/// Provider-agnostic AI request
/// </summary>
public class AIRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string? SystemMessage { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 1000;
    public Dictionary<string, object> AdditionalProperties { get; set; } = new();
}