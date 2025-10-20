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
    /// IChatAgent and IAIClient implementation backed by OpenRouter (OpenAI-compatible) using Microsoft.Extensions.AI.
    /// </summary>
    public sealed class OpenRouterChatAgent : BaseChatAgent, IChatAgent, IAIClient
    {
        private readonly HttpClient? _httpClient;

        /// <summary>
        /// Constructor for DI. For factory-based creation with tools, use CreateAsync.
        /// </summary>
        public OpenRouterChatAgent(
            IOptions<AppSettings> appOptions,
            IConfiguration configuration)
            : this(appOptions, configuration, string.Empty, null, null, null)
        {
        }

        /// <summary>
        /// Private constructor used by factory method and DI constructor.
        /// </summary>
        private OpenRouterChatAgent(
            IOptions<AppSettings> appOptions,
            IConfiguration configuration,
            string instructions,
            System.Collections.Generic.IEnumerable<object>? tools,
            IAgentThreadProvider? threadProvider,
            HttpClient? httpClient)
            : base(
                CreateChatClient(appOptions, configuration),
                "OpenRouterAgent",
                instructions,
                tools,
                threadProvider,
                appOptions.Value.LLM.OpenRouter.Model ?? "google/gemini-2.5-flash-lite-preview-09-2025")
        {
            _httpClient = httpClient;
        }

        private static IChatClient CreateChatClient(IOptions<AppSettings> appOptions, IConfiguration configuration)
        {
            var settings = appOptions.Value.LLM.OpenRouter;
            var endpoint = settings.Endpoint?.Trim() ?? "https://openrouter.ai/api/v1";
            var apiKey = configuration["OpenRouter:ApiKey"] ?? settings.ApiKey ?? string.Empty;
            var model = settings.Model ?? "google/gemini-2.5-flash-lite-preview-09-2025";

            var openAIClient = new OpenAIClient(
                new ApiKeyCredential(apiKey),
                new OpenAIClientOptions { Endpoint = new Uri(endpoint) });

            return openAIClient.GetChatClient(model).AsIChatClient();
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
            return Task.FromResult(new OpenRouterChatAgent(appOptions, configuration, instructions, tools, threadProvider, null));
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
        public string ProviderName => "OpenRouter";

        /// <inheritdoc />
        public Task<AIResponse> CallAsync(AIRequest request, CancellationToken cancellationToken = default)
            => CallAsyncCore(request, ProviderName, cancellationToken);

        /// <inheritdoc />
        public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        {
            // Lightweight health check - try to complete a minimal request
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