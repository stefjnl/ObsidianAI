namespace ObsidianAI.Web.Models;

/// <summary>
/// Represents the result of attempting to switch the active LLM provider at runtime.
/// </summary>
public sealed record ProviderSwitchResult(bool Success, string ActiveProvider, string ActiveModel, string? ErrorMessage);
