using ObsidianAI.Web.Models;

namespace ObsidianAI.Web.Services;

/// <summary>
/// Service interface for chat operations
/// </summary>
public interface IChatService
{
    Task<IEnumerable<QuickAction>> GetQuickActionsAsync();
    Task<string> GetLlmProviderAsync();
    Task<Guid> CreateConversationAsync();
    Task<IEnumerable<ConversationSummary>> ListConversationsAsync();
    Task<ConversationDetail> LoadConversationAsync(Guid conversationId);
    Task DeleteConversationAsync(Guid conversationId);
    Task<ConversationMetadata> ArchiveConversationAsync(Guid conversationId);
    Task<ConversationMetadata> UpdateConversationAsync(Guid conversationId, string? title, string? status);
    Task<string> ExportConversationAsync(Guid conversationId);
    Task<ModifyResponse> ModifyAsync(ModifyRequest request);
    Task<ReorganizeResponse> ReorganizeAsync(ReorganizeRequest request);
    Task UpdateMessageArtifactsAsync(Guid messageId, ArtifactUpdateRequest update);
    Task<VaultBrowserResponse> BrowseVaultAsync(string? path = null);
}
