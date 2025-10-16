using System;
using System.Threading;
using System.Threading.Tasks;
using ObsidianAI.Domain.Entities;
using ObsidianAI.Domain.Ports;

namespace ObsidianAI.Application.UseCases;

/// <summary>
/// Permanently deletes a conversation and its related data.
/// </summary>
public sealed class DeleteConversationUseCase
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IAgentThreadProvider _threadProvider;

    public DeleteConversationUseCase(IConversationRepository conversationRepository, IAgentThreadProvider threadProvider)
    {
        _conversationRepository = conversationRepository;
        _threadProvider = threadProvider ?? throw new ArgumentNullException(nameof(threadProvider));
    }

    /// <summary>
    /// Deletes the conversation with the provided identifier.
    /// </summary>
    public async Task ExecuteAsync(Guid conversationId, CancellationToken ct = default)
    {
        if (conversationId == Guid.Empty)
        {
            throw new ArgumentException("Conversation identifier cannot be empty.", nameof(conversationId));
        }

        Conversation? conversation = await _conversationRepository.GetByIdAsync(conversationId, includeMessages: false, ct).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(conversation?.ThreadId))
        {
            await _threadProvider.DeleteThreadAsync(conversation.ThreadId, ct).ConfigureAwait(false);
        }

        await _conversationRepository.DeleteAsync(conversationId, ct).ConfigureAwait(false);
    }
}
