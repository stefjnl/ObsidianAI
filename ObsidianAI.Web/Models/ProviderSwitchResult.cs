namespace ObsidianAI.Web.Models;

/// <summary>
/// Result of switching LLM provider
/// </summary>
public record ProviderSwitchResult(bool Success, string ActiveProvider, string ActiveModel, string? ErrorMessage);