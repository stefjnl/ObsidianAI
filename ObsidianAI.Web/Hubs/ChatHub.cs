using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using ObsidianAI.Application.Contracts;
using ObsidianAI.Application.UseCases;
using ObsidianAI.Domain.Entities;
using ObsidianAI.Domain.Models;
using ObsidianAI.Domain.Ports;
using ObsidianAI.Infrastructure.Configuration;
using ObsidianAI.Web.Services;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace ObsidianAI.Web.Hubs;

public class ChatHub : Hub, IDisposable
{
    private readonly StreamChatUseCase _streamChatUseCase;
    private readonly ILogger<ChatHub> _logger;
    private readonly IAIAgentFactory _agentFactory;
    private readonly IOptions<AppSettings> _appSettings;
    private bool _disposed;

    public ChatHub(
        StreamChatUseCase streamChatUseCase,
        ILogger<ChatHub> logger,
        IAIAgentFactory agentFactory,
        IOptions<AppSettings> appSettings)
    {
        _streamChatUseCase = streamChatUseCase;
        _logger = logger;
        _agentFactory = agentFactory;
        _appSettings = appSettings;
    }

    public async Task StreamMessage(string message, string? conversationId)
    {
        _logger.LogInformation("Processing streaming message for conversation: {ConversationId}", conversationId);

        try
        {
            var instructions = "You are a helpful AI assistant.";

            Guid? conversationGuid = null;
            if (!string.IsNullOrEmpty(conversationId) && Guid.TryParse(conversationId, out var guid))
            {
                conversationGuid = guid;
            }

            var input = new ChatInput(message);
            var persistenceContext = BuildPersistenceContext(conversationGuid, message);

            var fullResponse = new StringBuilder();
            var tokenBuffer = new StringBuilder();
            const int BufferFlushThreshold = 50;

            async Task FlushTokenBufferAsync(bool force = false)
            {
                if (tokenBuffer.Length == 0)
                {
                    return;
                }

                if (!force && tokenBuffer.Length < BufferFlushThreshold)
                {
                    return;
                }

                var payload = tokenBuffer.ToString();
                tokenBuffer.Clear();
                await Clients.Caller.SendAsync("ReceiveToken", payload);
            }

            int eventCount = 0;

            await foreach (var evt in _streamChatUseCase.ExecuteAsync(input, instructions, persistenceContext, Context.ConnectionAborted))
            {
                eventCount++;

                if (evt.Kind == ChatStreamEventKind.Metadata && !string.IsNullOrEmpty(evt.Metadata))
                {
                    await Clients.Caller.SendAsync("Metadata", evt.Metadata);
                }
                else if (evt.Kind == ChatStreamEventKind.Text && !string.IsNullOrEmpty(evt.Text))
                {
                    fullResponse.Append(evt.Text);
                    tokenBuffer.Append(evt.Text);
                    await FlushTokenBufferAsync();
                }
            }

            // Flush any remaining tokens and send completion
            await FlushTokenBufferAsync(force: true);
            var finalResponse = fullResponse.ToString();

            _logger.LogInformation("Final accumulated response (Length={Length}): '{EscapedResponse}'",
                finalResponse.Length,
                finalResponse.Length > 500 ? finalResponse.Substring(0, 500) + "..." : finalResponse);

            await Clients.Caller.SendAsync("MessageComplete", finalResponse);

            _logger.LogInformation("Message streaming complete ({EventCount} events)", eventCount);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Streaming message cancelled for connection {ConnectionId}", Context.ConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing streaming message");
            await Clients.Caller.SendAsync("Error", "An unexpected error occurred while processing your message.");
        }
    }

    private ConversationPersistenceContext BuildPersistenceContext(Guid? conversationId, string message)
    {
        var provider = ParseProvider(_appSettings.Value.LLM.Provider);
        var modelName = _agentFactory.GetModelName();
        return new ConversationPersistenceContext(conversationId, null, provider, modelName, message, null);
    }

    private static ConversationProvider ParseProvider(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return ConversationProvider.Unknown;
        }

        return provider.Trim().ToLowerInvariant() switch
        {
            "lmstudio" => ConversationProvider.LmStudio,
            "openrouter" => ConversationProvider.OpenRouter,
            "nanogpt" => ConversationProvider.NanoGPT,
            _ => ConversationProvider.Unknown
        };
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogError(exception, "Client disconnected with error: {ConnectionId}", Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public new void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        base.Dispose();
    }
}