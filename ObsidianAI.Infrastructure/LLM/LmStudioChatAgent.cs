using ObsidianAI.Domain.Ports;
using ObsidianAI.Domain.Models;
using ObsidianAI.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using OpenAI;
using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using System.Runtime.CompilerServices;

namespace ObsidianAI.Infrastructure.LLM
{
    /// <summary>
    /// IChatAgent implementation backed by LM Studio (OpenAI-compatible) using Microsoft.Extensions.AI.
    /// </summary>
    public sealed class LmStudioChatAgent : IChatAgent
    {
        private readonly IChatClient _chatClient;
        private readonly string _instructions;
        private readonly ChatClientAgent _agent;

        /// <summary>
        /// Private constructor - use CreateAsync factory method instead.
        /// </summary>
        private LmStudioChatAgent(IOptions<AppSettings> appOptions, string instructions, System.Collections.Generic.IEnumerable<object>? tools)
        {
            var settings = appOptions.Value.LLM.LMStudio;
            var endpoint = settings.Endpoint?.Trim() ?? "http://localhost:1234/v1";
            var apiKey = settings.ApiKey ?? "lm-studio";
            var model = settings.Model ?? "openai/gpt-oss-20b";

            var openAIClient = new OpenAIClient(
                new ApiKeyCredential(apiKey),
                new OpenAIClientOptions { Endpoint = new Uri(endpoint) });

            _chatClient = (IChatClient)openAIClient.GetChatClient(model);
            _instructions = instructions ?? string.Empty;

            // Create agent with tools (if provided)
            var aiTools = tools?.Cast<AITool>().ToArray() ?? Array.Empty<AITool>();
            _agent = new ChatClientAgent(
                _chatClient,
                name: "LmStudioAgent",
                instructions: _instructions,
                tools: aiTools);
        }

        /// <summary>
        /// Creates a new instance of the LmStudioChatAgent with optional MCP tools.
        /// </summary>
        public static async Task<LmStudioChatAgent> CreateAsync(IOptions<AppSettings> appOptions, string instructions, System.Collections.Generic.IEnumerable<object>? tools = null)
        {
            return await Task.FromResult(new LmStudioChatAgent(appOptions, instructions, tools));
        }

        /// <inheritdoc />
        public async Task<string> SendAsync(string message, CancellationToken ct = default)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));
            var response = await _agent.RunAsync(message).ConfigureAwait(false);
            return response?.Text ?? string.Empty;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<ChatStreamEvent> StreamAsync(string message, [EnumeratorCancellation] CancellationToken ct = default)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));

            var stream = _agent.RunStreamingAsync(message);
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
    }
}