using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Agents.AI;
using ObsidianAI.Domain.Models;
using ObsidianAI.Domain.Ports;
using ObsidianAI.Infrastructure.Configuration;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ObsidianAI.Infrastructure.LLM;

/// <summary>
/// IChatAgent implementation for NanoGPT deployments using the OpenAI-compatible pipeline.
/// </summary>
public sealed class NanoGptChatAgent : IChatAgent
{
    private readonly IChatClient _chatClient;
    private readonly ChatClientAgent _agent;
    private readonly IAgentThreadProvider? _threadProvider;

    private NanoGptChatAgent(
        IOptions<AppSettings> appOptions,
        IConfiguration configuration,
        string instructions,
        IEnumerable<object>? tools,
        IAgentThreadProvider? threadProvider)
    {
        var nanoGptSettings = appOptions.Value.LLM.NanoGPT ?? new NanoGptSettings();
        var endpoint = nanoGptSettings.Endpoint?.Trim();
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new InvalidOperationException("NanoGPT endpoint is not configured.");
        }

        var apiKey = configuration["NanoGpt:ApiKey"] ?? nanoGptSettings.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("NanoGPT API key is not configured. Set NanoGpt:ApiKey via user secrets or environment variables.");
        }

        var model = nanoGptSettings.Model;
        var client = new OpenAI.OpenAIClient(
            new System.ClientModel.ApiKeyCredential(apiKey),
            new OpenAI.OpenAIClientOptions { Endpoint = new Uri(endpoint) });

        _chatClient = client.GetChatClient(model).AsIChatClient();
        var aiTools = tools?.OfType<AITool>().ToArray() ?? Array.Empty<AITool>();
        _agent = new ChatClientAgent(
            _chatClient,
            name: "NanoGptAgent",
            instructions: instructions ?? string.Empty,
            tools: aiTools);
        _threadProvider = threadProvider;
    }

    public static Task<NanoGptChatAgent> CreateAsync(
        IOptions<AppSettings> appOptions,
        IConfiguration configuration,
        string instructions,
        IEnumerable<object>? tools = null,
        IAgentThreadProvider? threadProvider = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new NanoGptChatAgent(appOptions, configuration, instructions, tools, threadProvider));
    }

    public async Task<string> SendAsync(string message, string? threadId = null, CancellationToken ct = default)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        AgentThread? thread = null;
        if (!string.IsNullOrEmpty(threadId) && _threadProvider is not null)
        {
            thread = await _threadProvider.GetThreadAsync(threadId, ct).ConfigureAwait(false);
        }

        var response = thread is not null
            ? await _agent.RunAsync(message, thread, cancellationToken: ct).ConfigureAwait(false)
            : await _agent.RunAsync(message, cancellationToken: ct).ConfigureAwait(false);

        return response?.Text ?? string.Empty;
    }

    public async IAsyncEnumerable<ChatStreamEvent> StreamAsync(string message, string? threadId = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        AgentThread? thread = null;
        if (!string.IsNullOrEmpty(threadId) && _threadProvider is not null)
        {
            thread = await _threadProvider.GetThreadAsync(threadId, ct).ConfigureAwait(false);
        }

        var stream = thread is not null
            ? _agent.RunStreamingAsync(message, thread, cancellationToken: ct)
            : _agent.RunStreamingAsync(message, cancellationToken: ct);

        await foreach (var update in stream.WithCancellation(ct).ConfigureAwait(false))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                yield return ChatStreamEvent.TextChunk(update.Text);
            }

            if (update.Contents is null)
            {
                continue;
            }

            foreach (var content in update.Contents)
            {
                if (content is FunctionCallContent callContent && !string.IsNullOrEmpty(callContent.Name))
                {
                    var payload = ToolStreamingFormatter.CreatePayload(callContent.Name, "call", callContent.Arguments);
                    yield return ChatStreamEvent.ToolCall(callContent.Name, payload, "call");
                }
                else if (content is FunctionResultContent resultContent)
                {
                    var toolName = ResolveToolName(resultContent);
                    var payload = ToolStreamingFormatter.CreatePayload(toolName, "result", result: resultContent.Result);
                    yield return ChatStreamEvent.ToolCall(toolName, payload, "result");

                    var actionCardJson = ExtractActionCardFromToolResult(resultContent.Result);
                    if (actionCardJson is not null)
                    {
                        yield return ChatStreamEvent.ActionCardEvent(actionCardJson);
                    }
                }
            }
        }
    }

    public Task<AgentThread> CreateThreadAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_agent.GetNewThread());
    }

    private static string ResolveToolName(object source, string fallback = "unknown")
    {
        var name = source.GetType().GetProperty("Name")?.GetValue(source) as string;
        return string.IsNullOrWhiteSpace(name) ? fallback : name!;
    }

    private static string? ExtractActionCardFromToolResult(object? result)
    {
        if (result is null)
        {
            return null;
        }

        var type = result.GetType();
        var statusProp = type.GetProperty("Status");
        var actionCardProp = type.GetProperty("ActionCardJson");

        if (statusProp is null || actionCardProp is null)
        {
            return null;
        }

        var status = statusProp.GetValue(result)?.ToString();
        if (!string.Equals(status, "PENDING_CONFIRMATION", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return actionCardProp.GetValue(result)?.ToString();
    }
}
