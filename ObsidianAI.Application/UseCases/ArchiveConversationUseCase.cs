using System;
using System.Threading;
using System.Threading.Tasks;
using ObsidianAI.Application.DTOs;
using ObsidianAI.Application.Mappers;
using ObsidianAI.Domain.Ports;

namespace ObsidianAI.Application.UseCases;

/// <summary>
/// Marks an existing conversation as archived.
/// </summary>
public sealed class ArchiveConversationUseCase
{
    private readonly IConversationRepository _conversationRepository;

    public ArchiveConversationUseCase(IConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository;
    }

    /// <summary>
    /// Archives the specified conversation if it exists.
    /// </summary>
    public async Task<ConversationDetailDto?> ExecuteAsync(Guid conversationId, CancellationToken ct = default)
    {
        if (conversationId == Guid.Empty)
        {
            throw new ArgumentException("Conversation identifier cannot be empty.", nameof(conversationId));
        }

        var conversation = await _conversationRepository.GetByIdAsync(conversationId, includeMessages: false, ct).ConfigureAwait(false);
        if (conversation is null)
        {
            return null;
        }

        if (!conversation.IsArchived)
        {
            conversation.IsArchived = true;
            conversation.Touch();
            await _conversationRepository.UpdateAsync(conversation, ct).ConfigureAwait(false);
        }

        return conversation.ToDetailDto();
    }
}
