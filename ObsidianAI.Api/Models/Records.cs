namespace ObsidianAI.Api.Models;

public record ChatRequest(string Message, List<ChatMessage>? History = null);

public record ChatMessage(string Role, string Content, FileOperationData? FileOperation = null);

public record SearchRequest(string Query);

public record SearchResponse(List<SearchResult> Results);

public record SearchResult(string FilePath, float Score, string Content);

public record ReorganizeRequest(string Strategy);

public record ReorganizeResponse(string Status, int FilesAffected);

public record FileOperationData(string Action, string FilePath);

public record ModifyRequest(string Operation, string FilePath, string Content, string ConfirmationId);

public record ModifyResponse(bool Success, string Message, string FilePath);

/// <summary>
/// Strongly-typed configuration class for application settings.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// LLM configuration section.
    /// </summary>
    public required LlmSettings LLM { get; set; }
}

/// <summary>
/// LLM settings containing provider and specific provider configurations.
/// </summary>
public class LlmSettings
{
    /// <summary>
    /// The LLM provider to use (e.g., "LMStudio" or "OpenRouter").
    /// </summary>
    public required string Provider { get; set; }

    /// <summary>
    /// LMStudio-specific configuration.
    /// </summary>
    public required LmStudioSettings LMStudio { get; set; }

    /// <summary>
    /// OpenRouter-specific configuration.
    /// </summary>
    public required OpenRouterSettings OpenRouter { get; set; }
}

/// <summary>
/// LMStudio configuration settings.
/// </summary>
public class LmStudioSettings
{
    /// <summary>
    /// The endpoint URL for LMStudio.
    /// </summary>
    public required string Endpoint { get; set; }

    /// <summary>
    /// The API key for LMStudio.
    /// </summary>
    public required string ApiKey { get; set; }

    /// <summary>
    /// The model name for LMStudio.
    /// </summary>
    public required string Model { get; set; }
}

/// <summary>
 /// OpenRouter configuration settings.
/// </summary>
public class OpenRouterSettings
{
    /// <summary>
    /// The endpoint URL for OpenRouter.
    /// </summary>
    public required string Endpoint { get; set; }

    /// <summary>
    /// The API key for OpenRouter.
    /// </summary>
    public required string ApiKey { get; set; }

    /// <summary>
    /// The model name for OpenRouter.
    /// </summary>
    public required string Model { get; set; }
}