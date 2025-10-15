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
    public async Task<string> SendMessageAsync(string message)
    {
        try
        {
            _logger.LogInformation("Sending message to API: {Message}", message);
            
            var response = await _httpClient.PostAsJsonAsync("/chat", new { message });
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Received response from API");
            
            return result;
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
    public async Task<ChatMessage> SendMessageAndGetResponseAsync(string message)
    {
        try
        {
            _logger.LogInformation("Sending message to API: {Message}", message);
            
            var response = await _httpClient.PostAsJsonAsync("/chat", new { message });
            response.EnsureSuccessStatusCode();
            
            var apiResponse = await response.Content.ReadFromJsonAsync<ChatApiResponse>();
            
            var chatMessage = new ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                Content = apiResponse?.Text ?? "No response text received",
                Sender = MessageSender.AI,
                Timestamp = DateTime.UtcNow,
                FileOperation = apiResponse?.FileOperationResult
            };
            
            _logger.LogInformation("Received structured response from API");
            return chatMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to API");
            
            // Return a user-friendly error message
            return new ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                Content = "Sorry, I encountered an error processing your request. Please try again.",
                Sender = MessageSender.AI,
                Timestamp = DateTime.UtcNow
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
    /// Gets the conversation history.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <returns>A list of chat messages.</returns>
    public Task<List<ChatMessage>> GetConversationHistoryAsync(string sessionId)
    {
        _logger.LogInformation("Getting conversation history for session: {SessionId}", sessionId);
        
        // For now, return empty history as this would be implemented in a real API
        return Task.FromResult(new List<ChatMessage>());
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
    public string? Text { get; init; }
    public FileOperationData? FileOperationResult { get; init; }
}