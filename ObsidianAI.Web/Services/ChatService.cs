using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ObsidianAI.Web.Models;

namespace ObsidianAI.Web.Services;

/// <summary>
/// Implementation of the chat service that handles communication with the API.
/// </summary>
public class ChatService : IChatService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChatService> _logger;
    
    public ChatService(HttpClient httpClient, ILogger<ChatService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    
    /// <summary>
    /// Sends a message to the chat API and gets the response.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <returns>The response from the API.</returns>
    public async Task<string> SendMessageAsync(string message, Guid? conversationId = null)
    {
        try
        {
            _logger.LogInformation("Sending message to API: {Message}", message);

            var response = await _httpClient.PostAsJsonAsync("/chat", new { message, conversationId });
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ChatApiResponse>();
            _logger.LogInformation("Received response from API for conversation {ConversationId}", apiResponse?.ConversationId);

            return apiResponse?.Text ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to API");
            throw;
        }
    }

    /// <summary>
    /// Sends a message and gets a structured ChatMessage response for testing UI components.
    /// </summary>
    public async Task<ChatMessage> SendMessageAndGetResponseAsync(string message, Guid? conversationId = null)
    {
        try
        {
            _logger.LogInformation("Sending message to API: {Message}", message);

            var response = await _httpClient.PostAsJsonAsync("/chat", new { message, conversationId });
            response.EnsureSuccessStatusCode();
            
            var apiResponse = await response.Content.ReadFromJsonAsync<ChatApiResponse>();
            
            var messageId = (apiResponse?.AssistantMessageId ?? Guid.NewGuid()).ToString();
            var chatMessage = new ChatMessage
            {
                Id = messageId,
                ClientId = messageId,
                Content = apiResponse?.Text ?? "No response text received",
                Sender = MessageSender.AI,
                Timestamp = DateTime.UtcNow,
                FileOperation = apiResponse?.FileOperationResult,
                IsPending = false
            };
            
            _logger.LogInformation("Received structured response from API");
            return chatMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to API");
            
            // Return a user-friendly error message
            var fallbackId = Guid.NewGuid().ToString();
            return new ChatMessage
            {
                Id = fallbackId,
                ClientId = fallbackId,
                Content = "Sorry, I encountered an error processing your request. Please try again.",
                Sender = MessageSender.AI,
                Timestamp = DateTime.UtcNow,
                IsPending = false
            };
        }
    }
    
    /// <summary>
    /// Searches the vault for content matching the query.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <returns>A collection of search results.</returns>
    public async Task<SearchResultCollection> SearchVaultAsync(string query)
    {
        try
        {
            _logger.LogInformation("Searching vault with query: {Query}", query);
            
            var response = await _httpClient.PostAsJsonAsync("/vault/search", new { query });
            response.EnsureSuccessStatusCode();
            
            var searchResponse = await response.Content.ReadFromJsonAsync<SearchResponse>();
            
            // Convert to our model
            var results = searchResponse?.Results?.Select(r => new SearchResultData
            {
                Title = r.Title,
                FilePath = r.Path,
                Preview = r.Preview,
                Icon = GetIconForFileExtension(Path.GetExtension(r.Path)),
                FileExtension = Path.GetExtension(r.Path),
                FileSize = r.Size,
                LastModified = r.LastModified,
                Tags = r.Tags ?? new List<string>(),
                RelevanceScore = r.Score
            }).ToList() ?? new List<SearchResultData>();
            
            return new SearchResultCollection
            {
                Results = results,
                TotalCount = searchResponse?.TotalCount ?? 0,
                HasMore = (searchResponse?.TotalCount ?? 0) > results.Count,
                Query = query,
                SearchTimeMs = searchResponse?.SearchTimeMs ?? 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching vault");
            throw;
        }
    }
    
    /// <summary>
    /// Performs a reorganization operation on the vault.
    /// </summary>
    /// <param name="request">The reorganization request details.</param>
    /// <returns>The result of the reorganization operation.</returns>
    public async Task<ReorganizeResponse> ReorganizeAsync(ReorganizeRequest request)
    {
        try
        {
            _logger.LogInformation("Performing reorganization operation: {Operation}", request.Operation);
            
            var apiRequest = new
            {
                operation = request.Operation,
                fileOperations = request.FileOperations.Select(fo => new
                {
                    sourcePath = fo.SourcePath,
                    destinationPath = fo.DestinationPath,
                    operation = fo.Operation
                }),
                confirmationId = request.ConfirmationId
            };
            
            var response = await _httpClient.PostAsJsonAsync("/vault/reorganize", apiRequest);
            response.EnsureSuccessStatusCode();
            
            var apiResponse = await response.Content.ReadFromJsonAsync<ReorganizeApiResponse>();
            
            // Convert to our model
            return new ReorganizeResponse
            {
                Success = apiResponse?.Success ?? false,
                Message = apiResponse?.Message ?? string.Empty,
                Results = apiResponse?.Results?.Select(r => new OperationResult
                {
                    SourcePath = r.SourcePath,
                    DestinationPath = r.DestinationPath,
                    Success = r.Success,
                    Message = r.Message
                }).ToList() ?? new List<OperationResult>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing reorganization operation");
            throw;
        }
    }
    
    /// <summary>
    /// Creates a new note in the vault.
    /// </summary>
    /// <param name="request">The note creation request.</param>
    /// <returns>The result of the note creation operation.</returns>
    public async Task<CreateNoteResponse> CreateNoteAsync(CreateNoteRequest request)
    {
        try
        {
            _logger.LogInformation("Creating new note: {Title}", request.Title);
            
            var apiRequest = new
            {
                title = request.Title,
                content = request.Content,
                path = request.Path,
                template = request.Template,
                tags = request.Tags
            };
            
            var response = await _httpClient.PostAsJsonAsync("/vault/create", apiRequest);
            response.EnsureSuccessStatusCode();
            
            var apiResponse = await response.Content.ReadFromJsonAsync<CreateNoteApiResponse>();
            
            return new CreateNoteResponse
            {
                Success = apiResponse?.Success ?? false,
                Message = apiResponse?.Message ?? string.Empty,
                CreatedPath = apiResponse?.CreatedPath ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating note");
            throw;
        }
    }
    
    /// <summary>
    /// Gets the list of available quick actions.
    /// </summary>
    /// <returns>A list of quick action options.</returns>
    public Task<List<QuickAction>> GetQuickActionsAsync()
    {
        // For now, return hardcoded quick actions
        // In a real implementation, this might come from the API
        var quickActions = new List<QuickAction>
        {
            new() { Id = "search", Label = "Search vault", Prefix = "Search my vault for ", Icon = "🔍" },
            new() { Id = "create", Label = "Create note", Prefix = "Create a new note called ", Icon = "📝" },
            new() { Id = "reorganize", Label = "Reorganize", Prefix = "Reorganize my vault by ", Icon = "🔄" },
            new() { Id = "summarize", Label = "Summarize", Prefix = "Summarize ", Icon = "📄" }
        };
        
        return Task.FromResult(quickActions);
    }

    /// <summary>
    /// Retrieves paginated conversation summaries.
    /// </summary>
    public async Task<IReadOnlyList<ConversationSummary>> ListConversationsAsync(int skip = 0, int take = 20)
    {
        try
        {
            _logger.LogInformation("Listing conversations (skip={Skip}, take={Take})", skip, take);
            var response = await _httpClient.GetAsync($"/conversations?skip={skip}&take={take}");
            response.EnsureSuccessStatusCode();

            var summaries = await response.Content.ReadFromJsonAsync<List<ConversationSummaryApiResponse>>();
            return summaries?.Select(MapConversationSummary).ToList() ?? new List<ConversationSummary>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing conversations");
            throw;
        }
    }

    /// <summary>
    /// Loads a conversation with persisted messages.
    /// </summary>
    public async Task<ConversationDetail> LoadConversationAsync(Guid conversationId)
    {
        try
        {
            _logger.LogInformation("Loading conversation {ConversationId}", conversationId);
            var response = await _httpClient.GetAsync($"/conversations/{conversationId}");
            response.EnsureSuccessStatusCode();

            var detail = await response.Content.ReadFromJsonAsync<ConversationDetailApiResponse>();
            if (detail == null)
            {
                throw new InvalidOperationException($"Conversation {conversationId} could not be deserialized.");
            }

            var messages = detail.Messages.Select(MapConversationMessage).ToList();
            return new ConversationDetail(
                detail.Id,
                detail.Title,
                detail.CreatedAt,
                detail.UpdatedAt,
                detail.IsArchived,
                detail.Provider,
                detail.ModelName,
                messages);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning(ex, "Conversation {ConversationId} not found", conversationId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading conversation {ConversationId}", conversationId);
            throw;
        }
    }

    /// <summary>
    /// Creates a new conversation via the API.
    /// </summary>
    public async Task<Guid> CreateConversationAsync(string? title = null)
    {
        try
        {
            _logger.LogInformation("Creating conversation with title '{Title}'", title);
            var payload = new { title, userId = (string?)null };
            var response = await _httpClient.PostAsJsonAsync("/conversations", payload);
            response.EnsureSuccessStatusCode();

            var created = await response.Content.ReadFromJsonAsync<CreateConversationApiResponse>();
            if (created == null || created.Id == Guid.Empty)
            {
                throw new InvalidOperationException("API did not return a valid conversation identifier.");
            }

            return created.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating conversation");
            throw;
        }
    }

    /// <summary>
    /// Deletes a conversation in persistent storage.
    /// </summary>
    public async Task DeleteConversationAsync(Guid conversationId)
    {
        try
        {
            _logger.LogInformation("Deleting conversation {ConversationId}", conversationId);
            var response = await _httpClient.DeleteAsync($"/conversations/{conversationId}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting conversation {ConversationId}", conversationId);
            throw;
        }
    }

    /// <summary>
    /// Updates conversation metadata such as the title or archive state.
    /// </summary>
    public async Task<ConversationMetadata?> UpdateConversationAsync(Guid conversationId, string? title, bool? isArchived)
    {
        try
        {
            _logger.LogInformation("Updating conversation {ConversationId}", conversationId);
            var payload = new UpdateConversationApiRequest
            {
                Title = title,
                IsArchived = isArchived
            };

            var response = await _httpClient.PutAsJsonAsync($"/conversations/{conversationId}", payload);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Conversation {ConversationId} not found when updating", conversationId);
                return null;
            }

            response.EnsureSuccessStatusCode();
            var updated = await response.Content.ReadFromJsonAsync<UpdateConversationApiResponse>();
            if (updated == null)
            {
                throw new InvalidOperationException("API did not return updated conversation metadata.");
            }

            return new ConversationMetadata(
                updated.Id,
                updated.Title,
                updated.CreatedAt,
                updated.UpdatedAt,
                updated.IsArchived,
                updated.Provider,
                updated.ModelName,
                updated.MessageCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating conversation {ConversationId}", conversationId);
            throw;
        }
    }

    /// <summary>
    /// Archives an existing conversation.
    /// </summary>
    public async Task<ConversationMetadata?> ArchiveConversationAsync(Guid conversationId)
    {
        try
        {
            _logger.LogInformation("Archiving conversation {ConversationId}", conversationId);
            var response = await _httpClient.PostAsync($"/conversations/{conversationId}/archive", null);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Conversation {ConversationId} not found when archiving", conversationId);
                return null;
            }

            response.EnsureSuccessStatusCode();
            var archived = await response.Content.ReadFromJsonAsync<ArchiveConversationApiResponse>();
            if (archived == null)
            {
                throw new InvalidOperationException("API did not return archived conversation metadata.");
            }

            return new ConversationMetadata(
                archived.Id,
                archived.Title,
                archived.CreatedAt,
                archived.UpdatedAt,
                archived.IsArchived,
                archived.Provider,
                archived.ModelName,
                archived.MessageCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving conversation {ConversationId}", conversationId);
            throw;
        }
    }

    /// <summary>
    /// Downloads an exported conversation in JSON format.
    /// </summary>
    public async Task<string?> ExportConversationAsync(Guid conversationId)
    {
        try
        {
            _logger.LogInformation("Exporting conversation {ConversationId}", conversationId);
            var response = await _httpClient.GetAsync($"/conversations/{conversationId}/export");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Conversation {ConversationId} not found when exporting", conversationId);
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting conversation {ConversationId}", conversationId);
            throw;
        }
    }
    
    /// <summary>
    /// Gets the current LLM provider name from the backend.
    /// </summary>
    public async Task<string> GetLlmProviderAsync()
    {
        try
        {
            _logger.LogInformation("Fetching LLM provider from API");
            var response = await _httpClient.GetAsync("/api/llm/provider");
            response.EnsureSuccessStatusCode();

            var providerResponse = await response.Content.ReadFromJsonAsync<LlmProviderResponse>();
            var provider = providerResponse?.Provider?.Trim() ?? string.Empty;

            _logger.LogInformation("Active LLM provider: {Provider}", provider);
            return provider;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch LLM provider. Defaulting to empty string.");
            return string.Empty;
        }
    }

    private static ConversationSummary MapConversationSummary(ConversationSummaryApiResponse source)
    {
        return new ConversationSummary(
            source.Id,
            source.Title,
            source.UpdatedAt,
            source.MessageCount,
            source.Provider,
            source.ModelName);
    }

    private static ChatMessage MapConversationMessage(ConversationMessageApiResponse source)
    {
        var actionCard = MapActionCard(source.ActionCard);
        var fileOperation = source.FileOperation == null
            ? null
            : new FileOperationData
            {
                Action = source.FileOperation.Action,
                FilePath = source.FileOperation.FilePath
            };

        var identifier = source.Id.ToString();
        return new ChatMessage
        {
            Id = identifier,
            ClientId = identifier,
            Content = source.Content,
            Sender = MapSender(source.Role),
            Timestamp = source.Timestamp,
            ActionCard = actionCard,
            FileOperation = fileOperation,
            SearchResults = new List<SearchResultData>(),
            IsProcessing = source.IsProcessing,
            ProcessingType = ProcessingType.None,
            IsPending = false
        };
    }

    private static ActionCardData? MapActionCard(ActionCardApiResponse? source)
    {
        if (source == null)
        {
            return null;
        }

        return new ActionCardData
        {
            Id = source.Id.ToString(),
            Title = source.Title,
            Status = ParseEnum<ActionCardStatus>(source.Status, ActionCardStatus.Pending),
            StatusMessage = source.StatusMessage ?? string.Empty,
            OperationType = MapOperationType(source.Operation),
            Actions = source.PlannedActions?.Select(MapPlannedAction).ToList() ?? new List<PlannedAction>(),
            HasMoreActions = false,
            HiddenActionCount = 0
        };
    }

    private static PlannedAction MapPlannedAction(PlannedActionApiResponse source)
    {
        return new PlannedAction
        {
            Icon = "📄",
            Description = source.Description,
            Source = source.Source ?? string.Empty,
            Destination = source.Destination ?? string.Empty,
            Content = source.Content ?? string.Empty,
            Operation = source.Operation,
            Type = ParseEnum<ActionType>(source.Type, ActionType.Other)
        };
    }

    private static ActionOperationType MapOperationType(string operation)
    {
        return operation.ToLowerInvariant() switch
        {
            "move" => ActionOperationType.Move,
            "delete" => ActionOperationType.Delete,
            "create" => ActionOperationType.Create,
            "reorganize" => ActionOperationType.Reorganize,
            "search" => ActionOperationType.Search,
            _ => ActionOperationType.Other
        };
    }

    private static MessageSender MapSender(string role)
    {
        return role.Equals("user", StringComparison.OrdinalIgnoreCase)
            ? MessageSender.User
            : MessageSender.AI;
    }

    private static TEnum ParseEnum<TEnum>(string value, TEnum fallback) where TEnum : struct, Enum
    {
        return Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : fallback;
    }
    
    private static string GetIconForFileExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".md" => "📝",
            ".txt" => "📄",
            ".pdf" => "📕",
            ".doc" or ".docx" => "📘",
            ".xls" or ".xlsx" => "📗",
            ".ppt" or ".pptx" => "📙",
            ".jpg" or ".jpeg" or ".png" or ".gif" => "🖼️",
            ".mp4" or ".avi" or ".mov" => "🎬",
            ".mp3" or ".wav" or ".flac" => "🎵",
            ".zip" or ".rar" or ".7z" => "📦",
            _ => "📎"
        };
    }

    /// <inheritdoc />
    public async Task UpdateMessageArtifactsAsync(Guid messageId, MessageArtifactsUpdate update)
    {
        try
        {
            _logger.LogInformation("Persisting artifacts for message {MessageId}", messageId);

            var payload = new
            {
                actionCard = BuildActionCardPayload(update.ActionCard),
                fileOperation = BuildFileOperationPayload(update.FileOperation)
            };

            var response = await _httpClient.PostAsJsonAsync($"/messages/{messageId}/artifacts", payload);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Message {MessageId} not found when updating artifacts", messageId);
                return;
            }

            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating artifacts for message {MessageId}", messageId);
            throw;
        }
    }

    /// <summary>
    /// Performs a single-file modify operation (append/modify/delete/create) on the vault.
    /// </summary>
    /// <param name="request">The modify request details.</param>
    /// <returns>The result of the modify operation.</returns>
    public async Task<ModifyResponse> ModifyAsync(ModifyRequest request)
    {
        try
        {
            _logger.LogInformation("Performing modify operation: {Operation} on {FilePath}", request.Operation, request.FilePath);

            var apiRequest = new
            {
                operation = request.Operation,
                filePath = request.FilePath,
                content = request.Content,
                confirmationId = request.ConfirmationId
            };

            var response = await _httpClient.PostAsJsonAsync("/vault/modify", apiRequest);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<ModifyApiResponse>();

            return new ModifyResponse
            {
                Success = apiResponse?.Success ?? false,
                Message = apiResponse?.Message ?? string.Empty,
                FilePath = apiResponse?.FilePath ?? request.FilePath
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing modify operation");
            throw;
        }
    }

    private static object? BuildActionCardPayload(ActionCardUpdate? actionCard)
    {
        if (actionCard is null)
        {
            return null;
        }

        var plannedActions = actionCard.PlannedActions ?? Array.Empty<PlannedActionUpdate>();

        return new
        {
            id = actionCard.Id?.ToString(),
            title = actionCard.Title,
            status = actionCard.Status,
            operation = actionCard.Operation,
            statusMessage = actionCard.StatusMessage,
            createdAt = actionCard.CreatedAt,
            completedAt = actionCard.CompletedAt,
            plannedActions = plannedActions.Select(action => new
            {
                id = action.Id?.ToString(),
                type = action.Type,
                source = action.Source,
                destination = action.Destination,
                description = action.Description,
                operation = action.Operation,
                content = action.Content,
                sortOrder = action.SortOrder
            }).ToList()
        };
    }

    private static object? BuildFileOperationPayload(FileOperationUpdate? fileOperation)
    {
        if (fileOperation is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(fileOperation.Action) || string.IsNullOrWhiteSpace(fileOperation.FilePath))
        {
            return null;
        }

        return new
        {
            action = fileOperation.Action,
            filePath = fileOperation.FilePath,
            timestamp = fileOperation.Timestamp
        };
    }
}

// Internal API response models for deserialization
internal record SearchResponse
{
    public List<SearchResultItem> Results { get; init; } = new();
    public int TotalCount { get; init; }
    public long SearchTimeMs { get; init; }
}

internal record SearchResultItem
{
    public string Title { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public string Preview { get; init; } = string.Empty;
    public long Size { get; init; }
    public DateTime LastModified { get; init; }
    public List<string> Tags { get; init; } = new();
    public double Score { get; init; }
}

internal record ReorganizeApiResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<OperationResultItem> Results { get; init; } = new();
}

internal record OperationResultItem
{
    public string SourcePath { get; init; } = string.Empty;
    public string DestinationPath { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}

internal record CreateNoteApiResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string CreatedPath { get; init; } = string.Empty;
}

internal record LlmProviderResponse
{
    public string Provider { get; init; } = string.Empty;
}

// Record to model the expected JSON structure from the API
internal record ChatApiResponse
{
    public Guid ConversationId { get; init; }
    public Guid UserMessageId { get; init; }
    public Guid AssistantMessageId { get; init; }
    public string? Text { get; init; }
    public FileOperationData? FileOperationResult { get; init; }
}

internal record ConversationSummaryApiResponse
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; }
    public int MessageCount { get; init; }
    public string Provider { get; init; } = string.Empty;
    public string ModelName { get; init; } = string.Empty;
}

internal record ConversationDetailApiResponse
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public bool IsArchived { get; init; }
    public string Provider { get; init; } = string.Empty;
    public string ModelName { get; init; } = string.Empty;
    public List<ConversationMessageApiResponse> Messages { get; init; } = new();
}

internal record ConversationMessageApiResponse
{
    public Guid Id { get; init; }
    public string Role { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public bool IsProcessing { get; init; }
    public int? TokenCount { get; init; }
    public ActionCardApiResponse? ActionCard { get; init; }
    public FileOperationApiResponse? FileOperation { get; init; }
}

internal record ActionCardApiResponse
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Operation { get; init; } = string.Empty;
    public string? StatusMessage { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public List<PlannedActionApiResponse> PlannedActions { get; init; } = new();
}

internal record PlannedActionApiResponse
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string? Source { get; init; }
    public string? Destination { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Operation { get; init; } = string.Empty;
    public string? Content { get; init; }
    public int SortOrder { get; init; }
}

internal record FileOperationApiResponse
{
    public Guid Id { get; init; }
    public string Action { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

internal record CreateConversationApiResponse
{
    public Guid Id { get; init; }
}

internal record ModifyApiResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
}

internal record UpdateConversationApiRequest
{
    public string? Title { get; init; }
    public bool? IsArchived { get; init; }
}

internal record UpdateConversationApiResponse
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public bool IsArchived { get; init; }
    public string Provider { get; init; } = string.Empty;
    public string ModelName { get; init; } = string.Empty;
    public int MessageCount { get; init; }
}

internal record ArchiveConversationApiResponse
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public bool IsArchived { get; init; }
    public string Provider { get; init; } = string.Empty;
    public string ModelName { get; init; } = string.Empty;
    public int MessageCount { get; init; }
}