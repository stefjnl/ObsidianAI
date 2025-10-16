using System;
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
    Task<string> SendMessageAsync(string message, Guid? conversationId = null);

    /// <summary>
    /// Sends a message and gets a structured ChatMessage response, bypassing the streaming endpoint.
    /// Used for simulating direct responses like file operations.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <returns>A ChatMessage object, potentially with component data.</returns>
    Task<ChatMessage> SendMessageAndGetResponseAsync(string message, Guid? conversationId = null);
    
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
    /// Retrieves paginated conversation summaries.
    /// </summary>
    Task<IReadOnlyList<ConversationSummary>> ListConversationsAsync(int skip = 0, int take = 20);

    /// <summary>
    /// Loads a conversation with its persisted messages.
    /// </summary>
    Task<ConversationDetail> LoadConversationAsync(Guid conversationId);

    /// <summary>
    /// Creates a new conversation and returns its identifier.
    /// </summary>
    Task<Guid> CreateConversationAsync(string? title = null);

    /// <summary>
    /// Deletes a conversation from persistence.
    /// </summary>
    Task DeleteConversationAsync(Guid conversationId);

    /// <summary>
    /// Updates conversation metadata such as the title or archive state.
    /// </summary>
    Task<ConversationMetadata?> UpdateConversationAsync(Guid conversationId, string? title, bool? isArchived);

    /// <summary>
    /// Archives a conversation and returns the refreshed metadata.
    /// </summary>
    Task<ConversationMetadata?> ArchiveConversationAsync(Guid conversationId);

    /// <summary>
    /// Retrieves a serialized export payload for the conversation.
    /// </summary>
    Task<string?> ExportConversationAsync(Guid conversationId);
    
    /// <summary>
    /// Gets the conversation history.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <returns>A list of chat messages.</returns>
    Task<List<ChatMessage>> GetConversationHistoryAsync(string sessionId);
        
    /// <summary>
    /// Gets the current LLM provider name from the backend.
    /// </summary>
    Task<string> GetLlmProviderAsync();

    /// <summary>
    /// Performs a single-file modify operation (append/modify/delete/create) on the vault.
    /// </summary>
    /// <param name="request">The modify request details.</param>
    /// <returns>The result of the modify operation.</returns>
    Task<ModifyResponse> ModifyAsync(ModifyRequest request);

    /// <summary>
    /// Updates persisted artifacts for a specific message.
    /// </summary>
    /// <param name="messageId">Identifier of the message to update.</param>
    /// <param name="update">Artifact payload containing action card and/or file operation data.</param>
    Task UpdateMessageArtifactsAsync(Guid messageId, MessageArtifactsUpdate update);
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