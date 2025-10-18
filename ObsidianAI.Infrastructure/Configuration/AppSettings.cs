namespace ObsidianAI.Infrastructure.Configuration;

/// <summary>
/// Strongly-typed configuration class for application settings bound via the Options pattern.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// LLM configuration section.
    /// </summary>
    public required LlmSettings LLM { get; set; }

    /// <summary>
    /// Allowed file types for attachments.
    /// </summary>
    public string[] AllowedAttachmentTypes { get; set; } = [".txt", ".md", ".json"];
}