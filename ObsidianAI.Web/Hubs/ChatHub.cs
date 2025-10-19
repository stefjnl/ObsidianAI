using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using ObsidianAI.Application.Contracts;
using ObsidianAI.Application.UseCases;
using ObsidianAI.Domain.Entities;
using ObsidianAI.Domain.Models;
using ObsidianAI.Domain.Ports;
using ObsidianAI.Infrastructure.Configuration;
using ObsidianAI.Infrastructure.LLM;
using ObsidianAI.Web.Endpoints;
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
    private readonly IConversationRepository _conversationRepository;
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly IOptions<AppSettings> _appSettings;
    private bool _disposed;

    public ChatHub(
        StreamChatUseCase streamChatUseCase,
        ILogger<ChatHub> logger,
        IAIAgentFactory agentFactory,
        IConversationRepository conversationRepository,
        IAttachmentRepository attachmentRepository,
        IOptions<AppSettings> appSettings)
    {
        _streamChatUseCase = streamChatUseCase;
        _logger = logger;
        _agentFactory = agentFactory;
        _conversationRepository = conversationRepository;
        _attachmentRepository = attachmentRepository;
        _appSettings = appSettings;
    }

    public async Task StreamMessage(string message, string? conversationId)
    {
        _logger.LogInformation("Processing streaming message for conversation: {ConversationId}", conversationId);

        try
        {
            var instructions = AgentInstructions.ObsidianAssistant;

            Guid? conversationGuid = null;
            string? threadId = null;
            if (!string.IsNullOrEmpty(conversationId) && Guid.TryParse(conversationId, out var guid))
            {
                conversationGuid = guid;
                var conversation = await _conversationRepository.GetByIdAsync(guid, includeMessages: false, Context.ConnectionAborted);
                threadId = conversation?.ThreadId;
            }

            // Fetch attachments if conversation exists
            var attachments = new List<AttachmentContent>();
            if (conversationGuid.HasValue)
            {
                var attachmentEntities = await _attachmentRepository.GetByConversationIdAsync(conversationGuid.Value, Context.ConnectionAborted);
                attachments = attachmentEntities.Select(a => new AttachmentContent(a.Filename, a.Content, a.FileType)).ToList();
                if (attachments.Count > 0)
                {
                    _logger.LogInformation("Including {Count} attachments in chat context", attachments.Count);
                }
            }

            var input = new ChatInput(message, attachments);
            var persistenceContext = BuildPersistenceContext(conversationGuid, message, threadId);

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

                if (evt.Kind == ChatStreamEventKind.ToolCall)
                {
                    var toolName = evt.ToolName ?? "unknown";
                    var payload = !string.IsNullOrWhiteSpace(evt.ToolPayload)
                        ? evt.ToolPayload
                        : JsonSerializer.Serialize(new { name = toolName, phase = evt.ToolPhase ?? "unknown" });

                    _logger.LogInformation("Tool call event: {ToolName} (phase: {Phase})", toolName, evt.ToolPhase ?? "unknown");
                    await Clients.Caller.SendAsync("StatusUpdate", new { type = "tool_call", tool = payload });
                }
                else if (evt.Kind == ChatStreamEventKind.ActionCardMetadata && !string.IsNullOrEmpty(evt.ActionCardData))
                {
                    _logger.LogInformation("Received action_card event from reflection middleware");
                    await Clients.Caller.SendAsync("ActionCard", evt.ActionCardData);
                }
                else if (evt.Kind == ChatStreamEventKind.Metadata && !string.IsNullOrEmpty(evt.Metadata))
                {
                    if (!Streaming.UsageMetadataDiagnostics.TryLogUsage(evt.Metadata, _logger))
                    {
                        _logger.LogInformation("Metadata event: {Payload}", evt.Metadata);
                    }
                    await Clients.Caller.SendAsync("Metadata", evt.Metadata);
                }
                else if (evt.Kind == ChatStreamEventKind.Text && !string.IsNullOrEmpty(evt.Text))
                {
                    var decodedChunk = TextDecoderService.UnescapeJson(evt.Text);
                    var fullResponseStr = fullResponse.ToString();

                    // Add newline before list items if previous content exists and doesn't end with newline
                    if (fullResponse.Length > 0 &&
                        !fullResponseStr.EndsWith("\n") &&
                        decodedChunk.TrimStart().StartsWith("*"))
                    {
                        fullResponse.Append("\n\n");
                        tokenBuffer.Append("\n\n");
                        await FlushTokenBufferAsync(force: true);
                    }
                    // Add newline after list when transitioning to regular text
                    else if (fullResponse.Length > 0 &&
                             fullResponseStr.EndsWith("\n") &&
                             !fullResponseStr.EndsWith("\n\n") &&
                             !decodedChunk.TrimStart().StartsWith("*"))
                    {
                        var lastLine = fullResponseStr.Split('\n').Reverse().Skip(1).FirstOrDefault()?.TrimStart() ?? "";
                        if (lastLine.StartsWith("*"))
                        {
                            fullResponse.Append("\n");
                            tokenBuffer.Append("\n");
                            await FlushTokenBufferAsync(force: true);
                        }
                    }

                    fullResponse.Append(decodedChunk);
                    tokenBuffer.Append(decodedChunk);
                    await FlushTokenBufferAsync();
                }
            }

            // Flush any remaining tokens and send completion
            await FlushTokenBufferAsync(force: true);
            var finalResponse = TextDecoderService.UnescapeJson(fullResponse.ToString());

            var escapedFinal = finalResponse.Replace("\n", "\\n").Replace("\r", "\\r");
            _logger.LogInformation("Final accumulated response (Length={Length}): '{EscapedResponse}'",
                finalResponse.Length,
                escapedFinal.Length > 500 ? escapedFinal.Substring(0, 500) + "..." : escapedFinal);

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

    private ConversationPersistenceContext BuildPersistenceContext(Guid? conversationId, string message, string? threadId)
    {
        var provider = ParseProvider(_appSettings.Value.LLM.Provider);
        var modelName = _agentFactory.GetModelName();
        return new ConversationPersistenceContext(conversationId, null, provider, modelName, message, threadId);
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
        // Dispose any unmanaged resources here if needed
        // For example, if there were any subscriptions or timers
        base.Dispose();
    }
}