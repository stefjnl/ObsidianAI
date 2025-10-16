using System;
using System.Threading;
using System.Threading.Tasks;
using ObsidianAI.Application.DTOs;
using ObsidianAI.Application.Mappers;
using ObsidianAI.Domain.Ports;

namespace ObsidianAI.Application.UseCases;

/// <summary>
/// Updates mutable metadata for an existing conversation.
/// </summary>
public sealed class UpdateConversationUseCase
{
    private readonly IConversationRepository _conversationRepository;

    public UpdateConversationUseCase(IConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository;
    }

    /// <summary>
    /// Updates the conversation title and archived state.
    /// </summary>
    /// <param name="conversationId">Conversation identifier.</param>
    /// <param name="title">Optional updated title.</param>
    /// <param name="isArchived">Optional archived state.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated conversation DTO or <c>null</c> when not found.</returns>
    public async Task<ConversationDetailDto?> ExecuteAsync(Guid conversationId, string? title, bool? isArchived, CancellationToken ct = default)
    {
        if (conversationId == Guid.Empty)
        {
            throw new ArgumentException("Conversation identifier cannot be empty.", nameof(conversationId));
        }

        var conversation = await _conversationRepository.GetByIdAsync(conversationId, includeMessages: true, ct).ConfigureAwait(false);
        if (conversation is null)
        {
            return null;
        }

        var trimmedTitle = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        if (!string.IsNullOrEmpty(trimmedTitle))
        {
            conversation.Title = trimmedTitle.Length <= 80 ? trimmedTitle : trimmedTitle[..80] + "…";
        }

        if (isArchived.HasValue)
        {
            conversation.IsArchived = isArchived.Value;
        }

        conversation.Touch();
        await _conversationRepository.UpdateAsync(conversation, ct).ConfigureAwait(false);

        return conversation.ToDetailDto();
    }
}
