using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ObsidianAI.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ObsidianAI.Api.Services
{
    /// <summary>
    /// Application service that encapsulates the LLM agent and MCP tools for Obsidian assistance.
    /// Responsible for creating the underlying ChatClientAgent and exposing chat operations.
    /// </summary>
    public sealed class ObsidianAssistantService
    {
        private readonly ILlmClientFactory _llmFactory;
        private readonly McpClient _mcpClient;
        private readonly IChatClient _chatClient;
        private ChatClientAgent? _agent;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private volatile bool _isInitialized = false;

        /// <summary>
        /// Initializes the assistant service, creating the chat client and agent with available MCP tools.
        /// </summary>
        /// <param name="llmFactory">Factory to create the configured IChatClient.</param>
        /// <param name="mcpClient">Connected MCP client used to discover available tools.</param>
        public ObsidianAssistantService(ILlmClientFactory llmFactory, McpClient mcpClient)
        {
            _llmFactory = llmFactory ?? throw new ArgumentNullException(nameof(llmFactory));
            _mcpClient = mcpClient ?? throw new ArgumentNullException(nameof(mcpClient));

            _chatClient = _llmFactory.CreateChatClient();
        }

        private async Task InitializeAgentAsync()
        {
            if (_isInitialized)
                return;

            await _semaphore.WaitAsync();
            try
            {
                if (!_isInitialized)
                {
                    // Fetch tools asynchronously
                    var tools = await _mcpClient.ListToolsAsync();

                    _agent = _chatClient.CreateAIAgent(
                        name: "ObsidianAssistant",
                        instructions: "You help users query and organize their Obsidian vault. Use the available tools to search, read, and modify notes.",
                        tools: [.. tools.Cast<AITool>()]
                    );
                    
                    _isInitialized = true;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Handle a chat request and return the completed response text.
        /// </summary>
        /// <param name="request">Chat request containing message and optional history.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The model's response text.</returns>
        public async Task<string> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            
            // Ensure agent is initialized
            await InitializeAgentAsync();
            
            var response = await _agent!.RunAsync(request.Message);
            return response?.Text ?? string.Empty;
        }

        /// <summary>
        /// Stream a chat response as text chunks.
        /// </summary>
        /// <param name="request">Chat request containing message and optional history.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An async stream of text chunks.</returns>
        public async IAsyncEnumerable<string> StreamChatAsync(
            ChatRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            // Ensure agent is initialized
            await InitializeAgentAsync();
            
            var responseStream = _agent!.RunStreamingAsync(request.Message);
            var enumerableResponseStream = (IAsyncEnumerable<dynamic>)responseStream;

            await foreach (var update in enumerableResponseStream.WithCancellation(cancellationToken))
            {
                yield return update?.Text?.ToString() ?? string.Empty;
            }
        }
    }
}