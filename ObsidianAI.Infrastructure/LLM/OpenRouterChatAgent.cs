using ObsidianAI.Domain.Ports;
using ObsidianAI.Domain.Models;
using ObsidianAI.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using OpenAI;
using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using System.Runtime.CompilerServices;
using System.Threading;
using DomainChatResponse = global::ObsidianAI.Domain.Models.ChatResponse;

namespace ObsidianAI.Infrastructure.LLM
{
    /// <summary>
    /// IChatAgent implementation backed by OpenRouter (OpenAI-compatible) using Microsoft.Extensions.AI.
    /// </summary>
    public sealed class OpenRouterChatAgent : IChatAgent
    {
        private readonly IChatClient _chatClient;
        private readonly string _instructions;
        private readonly ChatClientAgent _agent;
        private readonly IAgentThreadProvider? _threadProvider;

        /// <summary>
        /// Private constructor - use CreateAsync factory method instead.
        /// </summary>
        private OpenRouterChatAgent(
            IOptions<AppSettings> appOptions,
            IConfiguration configuration,
            string instructions,
            System.Collections.Generic.IEnumerable<object>? tools,
            IAgentThreadProvider? threadProvider)
        {
            var settings = appOptions.Value.LLM.OpenRouter;
            var endpoint = settings.Endpoint?.Trim() ?? "https://openrouter.ai/api/v1";
            var apiKey = configuration["OpenRouter:ApiKey"] ?? settings.ApiKey ?? string.Empty;
            var model = settings.Model ?? "google/gemini-2.5-flash-lite-preview-09-2025";

            var openAIClient = new OpenAIClient(
                new ApiKeyCredential(apiKey),
                new OpenAIClientOptions { Endpoint = new Uri(endpoint) });

            _chatClient = openAIClient.GetChatClient(model).AsIChatClient();
            _instructions = instructions ?? string.Empty;

            // Create agent with tools (tools are already wrapped with middleware by factory)
            var aiTools = tools?.Cast<AITool>().ToArray() ?? Array.Empty<AITool>();
            _agent = new ChatClientAgent(
                _chatClient,
                name: "OpenRouterAgent",
                instructions: _instructions,
                tools: aiTools);
            _threadProvider = threadProvider;
        }

        /// <summary>
        /// Creates a new instance of the OpenRouterChatAgent with optional MCP tools.
        /// </summary>
        public static Task<OpenRouterChatAgent> CreateAsync(
            IOptions<AppSettings> appOptions,
            IConfiguration configuration,
            string instructions,
            System.Collections.Generic.IEnumerable<object>? tools = null,
            IAgentThreadProvider? threadProvider = null,
            CancellationToken ct = default)
        {
            return Task.FromResult(new OpenRouterChatAgent(appOptions, configuration, instructions, tools, threadProvider));
        }

        /// <inheritdoc />
        public async Task<DomainChatResponse> SendAsync(string message, string? threadId = null, CancellationToken ct = default)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));
            AgentThread? thread = null;
            if (!string.IsNullOrEmpty(threadId) && _threadProvider is not null)
            {
                thread = await _threadProvider.GetThreadAsync(threadId, ct).ConfigureAwait(false);
            }

            var response = thread is not null
            ? await _agent.RunAsync(message, thread, cancellationToken: ct).ConfigureAwait(false)
            : await _agent.RunAsync(message, cancellationToken: ct).ConfigureAwait(false);

            var text = response?.Text ?? string.Empty;
            var usage = response?.Usage;
            return new DomainChatResponse(text, usage);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<ChatStreamEvent> StreamAsync(string message, string? threadId = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));

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

                if (update.Contents is not null)
                {
                    foreach (var content in update.Contents)
                    {
                        if (content is FunctionCallContent fcc && !string.IsNullOrEmpty(fcc.Name))
                        {
                            var payload = global::ObsidianAI.Infrastructure.LLM.ToolStreamingFormatter.CreatePayload(fcc.Name, "call", arguments: fcc.Arguments);
                            yield return ChatStreamEvent.ToolCall(fcc.Name, payload, "call");
                        }
                        else if (content is FunctionResultContent frc)
                        {
                            var toolName = ResolveToolName(frc);
                            var payload = global::ObsidianAI.Infrastructure.LLM.ToolStreamingFormatter.CreatePayload(toolName, "result", result: frc.Result);
                            yield return ChatStreamEvent.ToolCall(toolName, payload, "result");

                            var actionCardJson = ExtractActionCardFromToolResult(frc.Result);
                            if (actionCardJson != null)
                            {
                                yield return ChatStreamEvent.ActionCardEvent(actionCardJson);
                            }
                        }
                        else if (content is UsageContent usageContent)
                        {
                            var usagePayload = global::ObsidianAI.Infrastructure.LLM.UsageMetadataBuilder.TryCreateUsagePayload(usageContent.Details);
                            if (!string.IsNullOrEmpty(usagePayload))
                            {
                                yield return ChatStreamEvent.MetadataEvent(usagePayload);
                            }
                        }
                    }
                }
            }
        }

        /// <inheritdoc />
        public Task<AgentThread> CreateThreadAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(_agent.GetNewThread());
        }

        private static string? ExtractActionCardFromToolResult(object? result)
        {
            if (result == null) return null;

            // Check if result is an anonymous object with Status = "PENDING_CONFIRMATION"
            var resultType = result.GetType();
            var statusProp = resultType.GetProperty("Status");
            var actionCardJsonProp = resultType.GetProperty("ActionCardJson");

            if (statusProp != null && actionCardJsonProp != null)
            {
                var status = statusProp.GetValue(result)?.ToString();
                if (status == "PENDING_CONFIRMATION")
                {
                    return actionCardJsonProp.GetValue(result)?.ToString();
                }
            }

            return null;
        }

        private static string ResolveToolName(object source, string fallback = "unknown")
        {
            var name = source.GetType().GetProperty("Name")?.GetValue(source) as string;
            return string.IsNullOrWhiteSpace(name) ? fallback : name!;
        }

    }
}