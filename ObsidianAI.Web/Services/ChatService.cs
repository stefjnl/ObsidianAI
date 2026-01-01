using System.Net.Http.Json;
using System.Text.Json;
using ObsidianAI.Domain.Ports;
using ObsidianAI.Web.Models;

namespace ObsidianAI.Web.Services;

/// <summary>
/// Chat service that delegates to REST API endpoints
/// </summary>
public class ChatService : IChatService
{
    private readonly HttpClient _httpClient;
    private readonly ILlmProviderRuntimeStore _runtimeStore;

    public ChatService(HttpClient httpClient, ILlmProviderRuntimeStore runtimeStore)
    {
        _httpClient = httpClient;
        _runtimeStore = runtimeStore;
    }

    public Task<IEnumerable<QuickAction>> GetQuickActionsAsync()
    {
        // Return hardcoded quick actions for now
        var actions = new List<QuickAction>
        {
            new("Summarize", "Summarize the key points from my notes"),
            new("Search", "Search my vault for "),
            new("Create Note", "Create a new note about ")
        };
        return Task.FromResult<IEnumerable<QuickAction>>(actions);
    }

    public Task<string> GetCurrentModelAsync() => Task.FromResult(_runtimeStore.CurrentModel);

    public Task<IReadOnlyList<ModelInfo>> GetAvailableModelsAsync() => Task.FromResult(_runtimeStore.GetAvailableModels());

    public Task<ModelSwitchResult> SwitchModelAsync(string modelIdentifier)
    {
        if (string.IsNullOrWhiteSpace(modelIdentifier))
        {
            return Task.FromResult(new ModelSwitchResult(false, _runtimeStore.CurrentModel, "Model identifier is required."));
        }

        if (_runtimeStore.TrySwitchModel(modelIdentifier, out var error))
        {
            return Task.FromResult(new ModelSwitchResult(true, _runtimeStore.CurrentModel, null));
        }

        return Task.FromResult(new ModelSwitchResult(false, _runtimeStore.CurrentModel, error ?? "Failed to switch model."));
    }

    // Legacy method for backward compatibility
    public Task<ProviderSwitchResult> SwitchLlmProviderAsync(string providerName)
    {
        // NanoGPT is the only provider - treat as no-op
        _runtimeStore.TrySwitchProvider(providerName, out var model, out var error);
        return Task.FromResult(new ProviderSwitchResult(true, "NanoGPT", model, null));
    }

    public async Task<Guid> CreateConversationAsync()
    {
        var request = new { Title = "New Conversation", UserId = (string?)null };
        var response = await _httpClient.PostAsJsonAsync("/conversations", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        return result.GetProperty("id").GetGuid();
    }

    public async Task<IEnumerable<ConversationSummary>> ListConversationsAsync()
    {
        var summaries = await _httpClient.GetFromJsonAsync<List<ConversationSummary>>("/conversations");
        return summaries ?? new List<ConversationSummary>();
    }

    public async Task<ConversationDetail> LoadConversationAsync(Guid conversationId)
    {
        var detail = await _httpClient.GetFromJsonAsync<ConversationDetail>($"/conversations/{conversationId}");
        return detail ?? new ConversationDetail(
            conversationId,
            "Untitled",
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            "Unknown",
            "Unknown",
            new List<ChatMessage>());
    }

    public async Task DeleteConversationAsync(Guid conversationId)
    {
        var response = await _httpClient.DeleteAsync($"/conversations/{conversationId}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<ConversationMetadata> ArchiveConversationAsync(Guid conversationId)
    {
        var response = await _httpClient.PatchAsJsonAsync($"/conversations/{conversationId}", new { status = "archived" });
        response.EnsureSuccessStatusCode();
        var metadata = await response.Content.ReadFromJsonAsync<ConversationMetadata>();
        return metadata ?? new ConversationMetadata(
            conversationId,
            "Archived",
            DateTime.UtcNow,
            DateTime.UtcNow,
            true,
            "Unknown",
            "Unknown",
            0);
    }

    public async Task<ConversationMetadata> UpdateConversationAsync(Guid conversationId, string? title, string? status)
    {
        var payload = new Dictionary<string, object?>();
        if (title != null) payload["title"] = title;
        if (status != null) payload["status"] = status;

        var response = await _httpClient.PatchAsJsonAsync($"/conversations/{conversationId}", payload);
        response.EnsureSuccessStatusCode();
        var metadata = await response.Content.ReadFromJsonAsync<ConversationMetadata>();
        return metadata ?? new ConversationMetadata(
            conversationId,
            title ?? "Updated",
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            "Unknown",
            "Unknown",
            0);
    }

    public async Task<string> ExportConversationAsync(Guid conversationId)
    {
        var response = await _httpClient.GetAsync($"/conversations/{conversationId}/export");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<ModifyResponse> ModifyAsync(ModifyRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/vault/modify", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ModifyResponse>();
        return result ?? new ModifyResponse { Success = false, Message = "No response" };
    }

    public async Task<ReorganizeResponse> ReorganizeAsync(ReorganizeRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/vault/reorganize", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ReorganizeResponse>();
        return result ?? new ReorganizeResponse { Success = false, Message = "No response" };
    }

    public async Task UpdateMessageArtifactsAsync(Guid messageId, ArtifactUpdateRequest update)
    {
        var response = await _httpClient.PatchAsJsonAsync($"/messages/{messageId}/artifacts", update);
        response.EnsureSuccessStatusCode();
    }

    public async Task<VaultBrowserResponse> BrowseVaultAsync(string? path = null)
    {
        var queryString = string.IsNullOrEmpty(path) ? "" : $"?path={Uri.EscapeDataString(path)}";
        var response = await _httpClient.GetFromJsonAsync<JsonElement>($"/vault/browse{queryString}");
        var itemsArray = response.GetProperty("items");
        var items = new List<VaultItemData>();
        
        foreach (var item in itemsArray.EnumerateArray())
        {
            var typeString = item.GetProperty("type").GetString() ?? "File";
            var itemType = Enum.TryParse<VaultItemType>(typeString, ignoreCase: true, out var parsedType) 
                ? parsedType 
                : VaultItemType.File;
            
            // Handle nullable size property
            long? sizeValue = null;
            if (item.TryGetProperty("size", out var sizeElement) && sizeElement.ValueKind == JsonValueKind.Number)
            {
                sizeValue = sizeElement.GetInt64();
            }
            
            // Handle nullable lastModified property
            DateTime? lastModifiedValue = null;
            if (item.TryGetProperty("lastModified", out var lmElement) && lmElement.ValueKind == JsonValueKind.String)
            {
                lastModifiedValue = lmElement.GetDateTime();
            }
            
            items.Add(new VaultItemData
            {
                Name = item.GetProperty("name").GetString() ?? "",
                Path = item.GetProperty("path").GetString() ?? "",
                Type = itemType,
                Extension = item.TryGetProperty("extension", out var ext) && ext.ValueKind == JsonValueKind.String ? ext.GetString() : null,
                Size = sizeValue,
                LastModified = lastModifiedValue,
                Icon = itemType == VaultItemType.Folder ? "📁" : "📄"
            });
        }
        
        return new VaultBrowserResponse
        {
            Items = items,
            CurrentPath = response.GetProperty("currentPath").GetString() ?? "/"
        };
    }
}
