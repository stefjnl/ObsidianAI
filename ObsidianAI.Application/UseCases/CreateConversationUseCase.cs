using System;
using System.Threading;
using System.Threading.Tasks;
using ObsidianAI.Domain.Entities;
using ObsidianAI.Domain.Ports;

namespace ObsidianAI.Application.UseCases;

/// <summary>
/// Use case responsible for creating new conversations in persistent storage.
/// </summary>
public sealed class CreateConversationUseCase
{
    private readonly IConversationRepository _conversationRepository;

    public CreateConversationUseCase(IConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository;
    }

    /// <summary>
    /// Creates a new conversation and returns its identifier.
    /// </summary>
    /// <param name="userId">Optional user identifier when multi-user support is enabled.</param>
    /// <param name="titleSource">Text used to derive the conversation title.</param>
    /// <param name="provider">LLM provider that will service this conversation.</param>
    /// <param name="modelName">LLM model name.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<Guid> ExecuteAsync(string? userId, string? titleSource, ConversationProvider provider, string modelName, CancellationToken ct = default)
    {
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = CreateTitle(titleSource),
            Provider = provider,
            ModelName = modelName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsArchived = false
        };

        await _conversationRepository.CreateAsync(conversation, ct).ConfigureAwait(false);
        return conversation.Id;
    }

    private static string CreateTitle(string? titleSource)
    {
        if (!string.IsNullOrWhiteSpace(titleSource))
        {
            var trimmed = titleSource.Trim();
            const int MaxLength = 80;
            if (trimmed.Length <= MaxLength)
            {
                return trimmed;
            }

            return trimmed.Substring(0, MaxLength) + "…";
        }

        return $"Chat - {DateTime.UtcNow:MMM d, yyyy HH:mm}";
    }
}
