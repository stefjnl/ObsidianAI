using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ObsidianAI.Domain.Ports;
using ObsidianAI.Infrastructure.Configuration;
using ObsidianAI.Web.Models;

namespace ObsidianAI.Web.Services;

/// <summary>
/// Chat service that delegates to REST API endpoints
/// </summary>
public class ChatService : IChatService
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<AppSettings> _appSettings;
    private readonly IAIAgentFactory _agentFactory;

    public ChatService(HttpClient httpClient, IOptions<AppSettings> appSettings, IAIAgentFactory agentFactory)
    {
        _httpClient = httpClient;
        _appSettings = appSettings;
        _agentFactory = agentFactory;
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

    public Task<string> GetLlmProviderAsync() => Task.FromResult(_appSettings.Value.LLM.Provider ?? "LMStudio");

    public Task<ProviderSwitchResult> SwitchLlmProviderAsync(string providerName)
    {
        // For minimal version, just return success with the requested provider
        var success = new ProviderSwitchResult(true, providerName, _agentFactory.GetModelName(), null);
        return Task.FromResult(success);
    }

    public Task<Guid> CreateConversationAsync()
    {
        return Task.FromResult(Guid.NewGuid());
    }

    public Task<IEnumerable<ConversationSummary>> ListConversationsAsync()
    {
        return Task.FromResult<IEnumerable<ConversationSummary>>(new List<ConversationSummary>());
    }

    public Task<ConversationDetail> LoadConversationAsync(Guid conversationId)
    {
        var detail = new ConversationDetail(
            conversationId,
            "Chat",
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            _appSettings.Value.LLM.Provider ?? "LMStudio",
            _agentFactory.GetModelName(),
            new List<ChatMessage>());
        return Task.FromResult(detail);
    }

    public Task DeleteConversationAsync(Guid conversationId)
    {
        // No-op for minimal version
        return Task.CompletedTask;
    }

    public Task<ConversationMetadata> ArchiveConversationAsync(Guid conversationId)
    {
        var metadata = new ConversationMetadata(
            conversationId,
            "Archived",
            DateTime.UtcNow,
            DateTime.UtcNow,
            true,
            _appSettings.Value.LLM.Provider ?? "LMStudio",
            _agentFactory.GetModelName(),
            0);
        return Task.FromResult(metadata);
    }

    public Task<ConversationMetadata> UpdateConversationAsync(Guid conversationId, string? title, string? status)
    {
        var metadata = new ConversationMetadata(
            conversationId,
            title ?? "Updated",
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            _appSettings.Value.LLM.Provider ?? "LMStudio",
            _agentFactory.GetModelName(),
            0);
        return Task.FromResult(metadata);
    }

    public Task<string> ExportConversationAsync(Guid conversationId)
    {
        return Task.FromResult("{\"messages\": []}");
    }

    public Task UpdateMessageArtifactsAsync(Guid messageId, ArtifactUpdateRequest update)
    {
        // No-op for minimal version
        return Task.CompletedTask;
    }
}
