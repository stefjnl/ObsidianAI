using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ObsidianAI.Application.Contracts;
using ObsidianAI.Domain.Entities;
using ObsidianAI.Domain.Models;
using ObsidianAI.Domain.Ports;

namespace ObsidianAI.Application.UseCases;

/// <summary>
/// Use case for starting a chat interaction with an AI agent and persisting the resulting messages.
/// </summary>
public class StartChatUseCase
{
    private readonly IAIAgentFactory _agentFactory;
    private readonly Domain.Services.IFileOperationExtractor _extractor;
    private readonly Application.Services.IMcpClientProvider? _mcpClientProvider;
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly ILogger<StartChatUseCase> _logger;

    public StartChatUseCase(
        IAIAgentFactory agentFactory,
        Domain.Services.IFileOperationExtractor extractor,
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        Application.Services.IMcpClientProvider? mcpClientProvider = null,
        ILogger<StartChatUseCase>? logger = null)
    {
        _agentFactory = agentFactory;
        _extractor = extractor;
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _mcpClientProvider = mcpClientProvider;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<StartChatUseCase>.Instance;
    }

    /// <summary>
    /// Executes the start chat use case.
    /// </summary>
    /// <param name="input">The chat input containing the user's message.</param>
    /// <param name="instructions">Instructions for the AI agent.</param>
    /// <param name="persistenceContext">Context required to persist conversation state.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result containing response text, message identifiers, and optional file operation.</returns>
    public async Task<Contracts.StartChatResult> ExecuteAsync(ChatInput input, string instructions, ConversationPersistenceContext persistenceContext, CancellationToken ct = default)
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
        var responseText = await agent.SendAsync(input.Message, ct).ConfigureAwait(false);
        var fileOperation = _extractor.Extract(responseText);

        var assistantMessage = await PersistAssistantMessageAsync(conversation.Id, responseText, fileOperation, ct).ConfigureAwait(false);

        await UpdateConversationMetadataAsync(conversation.Id, persistenceContext.TitleSource ?? input.Message, ct).ConfigureAwait(false);

        return new Contracts.StartChatResult(conversation.Id, userMessage.Id, assistantMessage.Id, responseText, fileOperation);
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

        try
        {
            await _messageRepository.AddAsync(message, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist user message {MessageId}", message.Id);
        }
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

        try
        {
            await _messageRepository.AddAsync(message, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist assistant message {MessageId}", message.Id);
        }
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