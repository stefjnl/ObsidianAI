using ObsidianAI.Web.Models;

namespace ObsidianAI.Web.Services;

/// <summary>
/// Interface for the chat service that handles communication with the API.
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Sends a message to the chat API and gets the response.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <returns>The response from the API.</returns>
    Task<string> SendMessageAsync(string message);
    
    /// <summary>
    /// Searches the vault for content matching the query.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <returns>A collection of search results.</returns>
    Task<SearchResultCollection> SearchVaultAsync(string query);
    
    /// <summary>
    /// Performs a reorganization operation on the vault.
    /// </summary>
    /// <param name="request">The reorganization request details.</param>
    /// <returns>The result of the reorganization operation.</returns>
    Task<ReorganizeResponse> ReorganizeAsync(ReorganizeRequest request);
    
    /// <summary>
    /// Creates a new note in the vault.
    /// </summary>
    /// <param name="request">The note creation request.</param>
    /// <returns>The result of the note creation operation.</returns>
    Task<CreateNoteResponse> CreateNoteAsync(CreateNoteRequest request);
    
    /// <summary>
    /// Gets the list of available quick actions.
    /// </summary>
    /// <returns>A list of quick action options.</returns>
    Task<List<QuickAction>> GetQuickActionsAsync();
    
    /// <summary>
    /// Gets the conversation history.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <returns>A list of chat messages.</returns>
    Task<List<ChatMessage>> GetConversationHistoryAsync(string sessionId);
}

/// <summary>
/// Request model for reorganization operations.
/// </summary>
public record ReorganizeRequest
{
    public string Operation { get; init; } = string.Empty;
    public List<FileOperation> FileOperations { get; init; } = new();
    public string ConfirmationId { get; init; } = string.Empty;
}

/// <summary>
/// Represents a file operation in a reorganization request.
/// </summary>
public record FileOperation
{
    public string SourcePath { get; init; } = string.Empty;
    public string DestinationPath { get; init; } = string.Empty;
    public string Operation { get; init; } = string.Empty; // move, delete, copy, etc.
}

/// <summary>
/// Response model for reorganization operations.
/// </summary>
public record ReorganizeResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<OperationResult> Results { get; init; } = new();
}

/// <summary>
/// Result of a single operation in a reorganization.
/// </summary>
public record OperationResult
{
    public string SourcePath { get; init; } = string.Empty;
    public string DestinationPath { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Request model for creating a new note.
/// </summary>
public record CreateNoteRequest
{
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public string Template { get; init; } = string.Empty;
    public List<string> Tags { get; init; } = new();
}

/// <summary>
/// Response model for note creation operations.
/// </summary>
public record CreateNoteResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string CreatedPath { get; init; } = string.Empty;
}

/// <summary>
/// Represents a quick action option.
/// </summary>
public record QuickAction
{
    public string Id { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Prefix { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
}