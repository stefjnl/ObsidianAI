using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ObsidianAI.Application.DTOs;
using ObsidianAI.Application.Mappers;
using ObsidianAI.Domain.Ports;

namespace ObsidianAI.Application.UseCases;

/// <summary>
/// Retrieves a paginated list of conversations for display in the UI.
/// </summary>
public sealed class ListConversationsUseCase
{
    private readonly IConversationRepository _conversationRepository;

    public ListConversationsUseCase(IConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository;
    }

    public async Task<IReadOnlyList<ConversationDto>> ExecuteAsync(string? userId, bool includeArchived, int skip, int take, CancellationToken ct = default)
    {
        var conversations = await _conversationRepository.GetAllAsync(userId, includeArchived, skip, take, ct).ConfigureAwait(false);
        return conversations.Select(c => c.ToDto()).ToList();
    }
}
