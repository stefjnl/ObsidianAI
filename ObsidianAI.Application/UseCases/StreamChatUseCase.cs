using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ObsidianAI.Application.Contracts;
using ObsidianAI.Domain.Entities;
using ObsidianAI.Domain.Models;
using ObsidianAI.Domain.Ports;

namespace ObsidianAI.Application.UseCases;

/// <summary>
/// Use case for streaming chat interactions with an AI agent while persisting conversation history.
/// </summary>
public class StreamChatUseCase
{
    private readonly IAIAgentFactory _agentFactory;
    private readonly Domain.Services.IFileOperationExtractor _extractor;
    private readonly Application.Services.IMcpClientProvider? _mcpClientProvider;
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;

    public StreamChatUseCase(
        IAIAgentFactory agentFactory,
        Domain.Services.IFileOperationExtractor extractor,
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        Application.Services.IMcpClientProvider? mcpClientProvider = null)
    {
        _agentFactory = agentFactory;
        _extractor = extractor;
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _mcpClientProvider = mcpClientProvider;
    }

    /// <summary>
    /// Executes the stream chat use case.
    /// </summary>
    /// <param name="input">The chat input containing the user's message.</param>
    /// <param name="instructions">Instructions for the AI agent.</param>
    /// <param name="persistenceContext">Context required to persist conversation state.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An asynchronous enumerable of chat stream events.</returns>
    public async IAsyncEnumerable<ChatStreamEvent> ExecuteAsync(
        ChatInput input,
        string instructions,
        ConversationPersistenceContext persistenceContext,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(persistenceContext);

        if (string.IsNullOrWhiteSpace(input.Message))
        {
            throw new ArgumentException("Message cannot be null or whitespace.", nameof(input));
        }

        var conversation = await EnsureConversationAsync(persistenceContext, input.Message, ct).ConfigureAwait(false);
        var userMessage = await PersistUserMessageAsync(conversation.Id, input.Message, ct).ConfigureAwait(false);

        IEnumerable<object>? tools = null;
        if (_mcpClientProvider != null)
        {
            var mcpClient = await _mcpClientProvider.GetClientAsync(ct).ConfigureAwait(false);
            if (mcpClient != null)
            {
                tools = await mcpClient.ListToolsAsync(cancellationToken: ct).ConfigureAwait(false);
            }
        }

        var agent = await _agentFactory.CreateAgentAsync(instructions, tools, ct).ConfigureAwait(false);

        var responseBuilder = new StringBuilder();

        await foreach (var evt in agent.StreamAsync(input.Message, ct).ConfigureAwait(false))
        {
            if (evt.Kind == ChatStreamEventKind.Text && !string.IsNullOrEmpty(evt.Text))
            {
                responseBuilder.Append(evt.Text);
            }

            yield return evt;
        }

        var finalResponse = responseBuilder.ToString();
        var fileOperation = _extractor.Extract(finalResponse);
        var assistantMessage = await PersistAssistantMessageAsync(conversation.Id, finalResponse, fileOperation, ct).ConfigureAwait(false);

        await UpdateConversationMetadataAsync(conversation.Id, persistenceContext.TitleSource ?? input.Message, ct).ConfigureAwait(false);

        var metadataPayload = JsonSerializer.Serialize(new
        {
            conversationId = conversation.Id,
            userMessageId = userMessage.Id,
            assistantMessageId = assistantMessage.Id,
            fileOperation = fileOperation == null ? null : new { fileOperation.Action, fileOperation.FilePath }
        });

        yield return ChatStreamEvent.MetadataEvent(metadataPayload);
    }

    private async Task<Conversation> EnsureConversationAsync(ConversationPersistenceContext context, string titleSource, CancellationToken ct)
    {
        Conversation? conversation = null;
        if (context.ConversationId.HasValue)
        {
            conversation = await _conversationRepository.GetByIdAsync(context.ConversationId.Value, includeMessages: false, ct).ConfigureAwait(false);
        }

        if (conversation != null)
        {
            return conversation;
        }

        var conversationId = context.ConversationId ?? Guid.NewGuid();
        conversation = new Conversation
        {
            Id = conversationId,
            UserId = context.UserId,
            Title = CreateTitle(titleSource),
            Provider = context.Provider,
            ModelName = context.ModelName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsArchived = false
        };

        await _conversationRepository.CreateAsync(conversation, ct).ConfigureAwait(false);
        return conversation;
    }

    private async Task<Message> PersistUserMessageAsync(Guid conversationId, string content, CancellationToken ct)
    {
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = MessageRole.User,
            Content = content,
            Timestamp = DateTime.UtcNow,
            IsProcessing = false
        };

        await _messageRepository.AddAsync(message, ct).ConfigureAwait(false);
        return message;
    }

    private async Task<Message> PersistAssistantMessageAsync(Guid conversationId, string content, Domain.Models.FileOperation? fileOperation, CancellationToken ct)
    {
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = MessageRole.Assistant,
            Content = content,
            Timestamp = DateTime.UtcNow,
            IsProcessing = false
        };

        if (fileOperation != null)
        {
            message.FileOperation = new FileOperationRecord
            {
                Id = Guid.NewGuid(),
                MessageId = message.Id,
                Action = fileOperation.Action,
                FilePath = fileOperation.FilePath,
                Timestamp = DateTime.UtcNow
            };
        }

        await _messageRepository.AddAsync(message, ct).ConfigureAwait(false);
        return message;
    }

    private async Task UpdateConversationMetadataAsync(Guid conversationId, string titleSource, CancellationToken ct)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId, includeMessages: false, ct).ConfigureAwait(false);
        if (conversation == null)
        {
            return;
        }

        conversation.UpdatedAt = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(conversation.Title) || conversation.Title.Equals("New Conversation", StringComparison.Ordinal))
        {
            conversation.Title = CreateTitle(titleSource);
        }

        await _conversationRepository.UpdateAsync(conversation, ct).ConfigureAwait(false);
    }

    private static string CreateTitle(string? titleSource)
    {
        if (string.IsNullOrWhiteSpace(titleSource))
        {
            return "New Conversation";
        }

        var trimmed = titleSource.Trim();
        const int MaxLength = 80;
        if (trimmed.Length <= MaxLength)
        {
            return trimmed;
        }

        return trimmed.Substring(0, MaxLength) + "…";
    }
}