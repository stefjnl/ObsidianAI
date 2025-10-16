using System;
using System.Threading;
using System.Threading.Tasks;
using ObsidianAI.Domain.Ports;

namespace ObsidianAI.Application.UseCases;

/// <summary>
/// Permanently deletes a conversation and its related data.
/// </summary>
public sealed class DeleteConversationUseCase
{
    private readonly IConversationRepository _conversationRepository;

    public DeleteConversationUseCase(IConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository;
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

        await _conversationRepository.DeleteAsync(conversationId, ct).ConfigureAwait(false);
    }
}
