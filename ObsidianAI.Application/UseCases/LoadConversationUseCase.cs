using System;
using System.Threading;
using System.Threading.Tasks;
using ObsidianAI.Application.DTOs;
using ObsidianAI.Application.Mappers;
using ObsidianAI.Domain.Ports;

namespace ObsidianAI.Application.UseCases;

/// <summary>
/// Use case for loading a persisted conversation with its messages.
/// </summary>
public sealed class LoadConversationUseCase
{
    private readonly IConversationRepository _conversationRepository;

    public LoadConversationUseCase(IConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository;
    }

    /// <summary>
    /// Retrieves a conversation with messages and converts it to a DTO.
    /// </summary>
    public async Task<ConversationDetailDto?> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var conversation = await _conversationRepository.GetByIdAsync(id, includeMessages: true, ct).ConfigureAwait(false);
        return conversation?.ToDetailDto();
    }
}
