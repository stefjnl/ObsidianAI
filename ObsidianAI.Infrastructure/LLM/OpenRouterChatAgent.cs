using ObsidianAI.Domain.Ports;
using ObsidianAI.Domain.Models;
using ObsidianAI.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using OpenAI;
using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

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
            string instructions,
            System.Collections.Generic.IEnumerable<object>? tools,
            IAgentThreadProvider? threadProvider)
        {
            var settings = appOptions.Value.LLM.OpenRouter;
            var endpoint = settings.Endpoint?.Trim() ?? "https://openrouter.ai/api/v1";
            var apiKey = settings.ApiKey ?? string.Empty;
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
            string instructions,
            System.Collections.Generic.IEnumerable<object>? tools = null,
            IAgentThreadProvider? threadProvider = null,
            CancellationToken ct = default)
        {
            return Task.FromResult(new OpenRouterChatAgent(appOptions, instructions, tools, threadProvider));
        }

        /// <inheritdoc />
        public async Task<string> SendAsync(string message, string? threadId = null, CancellationToken ct = default)
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

            return response?.Text ?? string.Empty;
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
                            yield return ChatStreamEvent.ToolCall(fcc.Name);
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

    }
}