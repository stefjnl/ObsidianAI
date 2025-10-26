using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ObsidianAI.Application.Contracts;
using ObsidianAI.Domain.Models;
using ObsidianAI.Domain.Ports;

namespace ObsidianAI.Application.UseCases;

/// <summary>
/// Use case for streaming chat interactions with an AI agent.
/// </summary>
public class StreamChatUseCase
{
    private readonly IAIAgentFactory _agentFactory;
    private readonly ILogger<StreamChatUseCase> _logger;

    public StreamChatUseCase(
        IAIAgentFactory agentFactory,
        ILogger<StreamChatUseCase>? logger = null)
    {
        _agentFactory = agentFactory;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<StreamChatUseCase>.Instance;
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

        var conversationId = persistenceContext.ConversationId ?? Guid.NewGuid();

        var agent = await _agentFactory.CreateAgentAsync(instructions, null, null, ct).ConfigureAwait(false);

        var initialMetadataPayload = JsonSerializer.Serialize(new
        {
            conversationId = conversationId,
            userMessageId = Guid.NewGuid()
        });
        yield return ChatStreamEvent.MetadataEvent(initialMetadataPayload);

        var responseBuilder = new StringBuilder();

        await foreach (var evt in agent.StreamAsync(input.Message, null, ct).ConfigureAwait(false))
        {
            if (evt.Kind == ChatStreamEventKind.Text && !string.IsNullOrEmpty(evt.Text))
            {
                responseBuilder.Append(evt.Text);
            }

            yield return evt;
        }

        var finalResponse = responseBuilder.ToString();

        var metadataPayload = JsonSerializer.Serialize(new
        {
            conversationId = conversationId,
            userMessageId = Guid.NewGuid(),
            assistantMessageId = Guid.NewGuid()
        });

        yield return ChatStreamEvent.MetadataEvent(metadataPayload);
    }
}