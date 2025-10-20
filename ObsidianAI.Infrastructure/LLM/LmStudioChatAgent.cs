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
using DomainChatResponse = global::ObsidianAI.Domain.Models.ChatResponse;

namespace ObsidianAI.Infrastructure.LLM
{
    /// <summary>
    /// IChatAgent and IAIClient implementation backed by LM Studio (OpenAI-compatible) using Microsoft.Extensions.AI.
    /// </summary>
    public sealed class LmStudioChatAgent : BaseChatAgent, IChatAgent, IAIClient
    {
        /// <summary>
        /// Private constructor - use CreateAsync factory method instead.
        /// </summary>
        private LmStudioChatAgent(
            IOptions<AppSettings> appOptions,
            string instructions,
            System.Collections.Generic.IEnumerable<object>? tools,
            IAgentThreadProvider? threadProvider)
            : base(
                CreateChatClient(appOptions),
                "LmStudioAgent",
                instructions,
                tools,
                threadProvider,
                appOptions.Value.LLM.LMStudio.Model ?? "openai/gpt-oss-20b")
        {
        }

        private static IChatClient CreateChatClient(IOptions<AppSettings> appOptions)
        {
            var settings = appOptions.Value.LLM.LMStudio;
            var endpoint = settings.Endpoint?.Trim() ?? "http://localhost:1234/v1";
            var apiKey = settings.ApiKey ?? "lm-studio";
            var model = settings.Model ?? "openai/gpt-oss-20b";

            var openAIClient = new OpenAIClient(
                new ApiKeyCredential(apiKey),
                new OpenAIClientOptions { Endpoint = new Uri(endpoint) });

            return openAIClient.GetChatClient(model).AsIChatClient();
        }

        /// <summary>
        /// Creates a new instance of the LmStudioChatAgent with optional MCP tools.
        /// </summary>
        public static Task<LmStudioChatAgent> CreateAsync(
            IOptions<AppSettings> appOptions,
            string instructions,
            System.Collections.Generic.IEnumerable<object>? tools = null,
            IAgentThreadProvider? threadProvider = null,
            CancellationToken ct = default)
        {
            return Task.FromResult(new LmStudioChatAgent(appOptions, instructions, tools, threadProvider));
        }

        // ========================================================================
        // IChatAgent Implementation (delegates to BaseChatAgent core methods)
        // ========================================================================

        /// <inheritdoc />
        public Task<DomainChatResponse> SendAsync(string message, string? threadId = null, CancellationToken ct = default)
            => SendAsyncCore(message, threadId, ct);

        /// <inheritdoc />
        public IAsyncEnumerable<ChatStreamEvent> StreamAsync(string message, string? threadId = null, CancellationToken ct = default)
            => StreamAsyncCore(message, threadId, ct);

        /// <inheritdoc />
        public Task<AgentThread> CreateThreadAsync(CancellationToken ct = default)
            => CreateThreadAsyncCore(ct);

        // ========================================================================
        // IAIClient Implementation (delegates to BaseChatAgent core methods)
        // ========================================================================

        /// <inheritdoc />
        public string ProviderName => "LMStudio";

        /// <inheritdoc />
        public Task<AIResponse> CallAsync(AIRequest request, CancellationToken cancellationToken = default)
            => CallAsyncCore(request, ProviderName, cancellationToken);

        /// <inheritdoc />
        public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var testRequest = new AIRequest
                {
                    Prompt = "test",
                    SystemMessage = "",
                    MaxTokens = 10
                };
                await CallAsync(testRequest, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public Task<IEnumerable<string>> GetModelsAsync(CancellationToken cancellationToken = default)
            => GetModelsCoreAsync(cancellationToken);
    }
}