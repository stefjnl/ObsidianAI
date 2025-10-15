using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ObsidianAI.Api.Models;
using ObsidianAI.Application.Services;
using ObsidianAI.Infrastructure.LLM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
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
    private readonly IMcpClientProvider _mcpClientProvider;
        private readonly IChatClient _chatClient;
        private ChatClientAgent? _agent;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private volatile bool _isInitialized = false;
        private readonly string _instructions;
        private readonly ILogger<ObsidianAssistantService>? _logger;
        private const string DefaultInstructions = "You help users query and organize their Obsidian vault. Use the available tools to search, read, and modify notes.";

        /// <summary>
        /// Initializes the assistant service, creating the chat client and agent with available MCP tools.
        /// </summary>
        /// <param name="llmFactory">Factory to create the configured IChatClient.</param>
        /// <param name="mcpClientProvider">Provider used to access the MCP client for tool discovery.</param>
        public ObsidianAssistantService(ILlmClientFactory llmFactory, IMcpClientProvider mcpClientProvider)
        {
            _llmFactory = llmFactory ?? throw new ArgumentNullException(nameof(llmFactory));
            _mcpClientProvider = mcpClientProvider ?? throw new ArgumentNullException(nameof(mcpClientProvider));
            _chatClient = _llmFactory.CreateChatClient();
            _instructions = DefaultInstructions;
            _logger = null;
        }

        public ObsidianAssistantService(ILlmClientFactory llmFactory, IMcpClientProvider mcpClientProvider, string instructions)
        {
            _llmFactory = llmFactory ?? throw new ArgumentNullException(nameof(llmFactory));
            _mcpClientProvider = mcpClientProvider ?? throw new ArgumentNullException(nameof(mcpClientProvider));
            _chatClient = _llmFactory.CreateChatClient();
            _instructions = string.IsNullOrWhiteSpace(instructions) ? DefaultInstructions : instructions;
            _logger = null;
        }

        public ObsidianAssistantService(ILlmClientFactory llmFactory, IMcpClientProvider mcpClientProvider, string instructions, ILogger<ObsidianAssistantService> logger)
        {
            _llmFactory = llmFactory ?? throw new ArgumentNullException(nameof(llmFactory));
            _mcpClientProvider = mcpClientProvider ?? throw new ArgumentNullException(nameof(mcpClientProvider));
            _chatClient = _llmFactory.CreateChatClient();
            _instructions = string.IsNullOrWhiteSpace(instructions) ? DefaultInstructions : instructions;
            _logger = logger;
        }

        private async Task InitializeAgentAsync(CancellationToken cancellationToken)
        {
            if (_isInitialized)
                return;

            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (!_isInitialized)
                {
                    var tools = Array.Empty<object>();
                    var mcpClient = await _mcpClientProvider.GetClientAsync(cancellationToken).ConfigureAwait(false);
                    if (mcpClient != null)
                    {
                        var discovered = await mcpClient.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                        tools = discovered.ToArray();
                    }
                    else
                    {
                        _logger?.LogWarning("MCP client unavailable. Initializing assistant without tools.");
                    }

                    _agent = _chatClient.CreateAIAgent(
                        name: "ObsidianAssistant",
                        instructions: _instructions,
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
            await InitializeAgentAsync(cancellationToken).ConfigureAwait(false);

            var agent = _agent ?? throw new InvalidOperationException("Agent not initialized");
            var response = await agent.RunAsync(request.Message);
            return response?.Text ?? string.Empty;
        }

        /// <summary>
        /// Stream a chat response as text chunks.
        /// </summary>
        /// <param name="request">Chat request containing message and optional history.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An async stream of text chunks.</returns>
        public async IAsyncEnumerable<ObsidianAI.Api.Models.ChatMessage> StreamChatAsync(
            ChatRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            // Ensure agent is initialized
            await InitializeAgentAsync(cancellationToken).ConfigureAwait(false);

            var agent = _agent ?? throw new InvalidOperationException("Agent not initialized");

            _logger?.LogInformation("Starting RunStreamingAsync for message: {Message}", request.Message);
            var responseStream = agent.RunStreamingAsync(request.Message);
            var updateCount = 0;

            await foreach (var update in responseStream.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                updateCount++;

                // Log the EXACT content with escaped characters to see newlines
                if (!string.IsNullOrEmpty(update.Text))
                {
                    var escapedText = update.Text
                        .Replace("\n", "\\n")
                        .Replace("\r", "\\r")
                        .Replace("\t", "\\t");
                    _logger?.LogInformation("Update #{Count}: RAW='{Escaped}' (Length={Length})",
                        updateCount, escapedText.Length > 100 ? escapedText.Substring(0, 100) + "..." : escapedText, update.Text.Length);
                }

                _logger?.LogDebug("Received update #{Count}: Text={HasText}, Contents={ContentCount}",
                    updateCount, !string.IsNullOrEmpty(update.Text), update.Contents?.Count ?? 0);

                // The update.Text contains INCREMENTAL tokens (not cumulative)
                if (!string.IsNullOrEmpty(update.Text))
                {
                    yield return new ObsidianAI.Api.Models.ChatMessage("assistant", update.Text);
                }

                // Check for tool calls in update.Contents
                if (update.Contents != null)
                {
                    foreach (var content in update.Contents)
                    {
                        // Check if this is a function call
                        if (content is Microsoft.Extensions.AI.FunctionCallContent functionCall)
                        {
                            _logger?.LogInformation("Tool call detected: {ToolName}", functionCall.Name);
                            // Return a special message indicating tool call
                            yield return new ObsidianAI.Api.Models.ChatMessage("tool_call", functionCall.Name ?? "unknown");
                        }
                    }
                }
            }

            _logger?.LogInformation("RunStreamingAsync complete. Total updates: {Count}", updateCount);
        }
    }
}