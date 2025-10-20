using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using ObsidianAI.Application.Contracts;
using ObsidianAI.Application.Services;
using ObsidianAI.Domain.Models;
using ObsidianAI.Domain.Ports;

namespace ObsidianAI.Application.UseCases;

public class StreamChatUseCase
{
    private readonly IMcpToolCatalog _toolCatalog;
    private readonly IToolSelectionStrategy _toolSelection;
    private readonly IAIAgentFactory _agentFactory;
    private readonly IAgentThreadProvider _threadProvider;
    private readonly ILogger<StreamChatUseCase> _logger;

    public StreamChatUseCase(
        IMcpToolCatalog toolCatalog,
        IToolSelectionStrategy toolSelection,
        IAIAgentFactory agentFactory,
        IAgentThreadProvider threadProvider,
        ILogger<StreamChatUseCase> logger)
    {
        _toolCatalog = toolCatalog;
        _toolSelection = toolSelection;
        _agentFactory = agentFactory;
        _threadProvider = threadProvider;
        _logger = logger;
    }

    public async Task<IAsyncEnumerable<ChatStreamEvent>> ExecuteAsync(
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Analyze query to determine needed servers
        var selectedServers = await _toolSelection.SelectServersForQueryAsync(
            userMessage,
            cancellationToken);

        // Step 2: Load ONLY tools from selected servers
        var tools = await _toolCatalog.GetToolsFromServersAsync(
            selectedServers,
            cancellationToken);

        _logger.LogInformation(
            "Query: '{Query}' → Selected {ServerCount} servers, loaded {ToolCount} tools",
            userMessage,
            selectedServers.Count(),
            tools.Count());

        // Step 3: Create agent with tools and stream
        var agent = await _agentFactory.CreateAgentAsync("", tools.Cast<object>(), _threadProvider, cancellationToken).ConfigureAwait(false);
        return agent.StreamAsync(userMessage, ct: cancellationToken);
    }

    /// <summary>
    /// Streams chat using the richer request context used by Web callers (compatible with existing call sites).
    /// </summary>
    /// <param name="input">Chat input including message and optional attachments.</param>
    /// <param name="instructions">Agent instructions (currently informational for logging).</param>
    /// <param name="persistenceContext">Conversation persistence context providing provider/model/thread info.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async stream of ChatStreamEvent items.</returns>
    public async IAsyncEnumerable<ChatStreamEvent> ExecuteAsync(
        ChatInput input,
        string instructions,
        ConversationPersistenceContext persistenceContext,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Determine which servers to include for this query
        var selectedServers = await _toolSelection.SelectServersForQueryAsync(
            input.Message,
            cancellationToken).ConfigureAwait(false);

        // Optionally fetch tools (for logging/metrics)
        var tools = await _toolCatalog.GetToolsFromServersAsync(
            selectedServers,
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Streaming query: Selected {ServerCount} servers, loaded {ToolCount} tools",
            selectedServers.Count(),
            tools.Count());

        // Use provider-agnostic agent to stream, preserving thread when available
        var agent = await _agentFactory.CreateAgentAsync(instructions, tools.Cast<object>(), _threadProvider, cancellationToken).ConfigureAwait(false);
        var stream = agent.StreamAsync(input.Message, persistenceContext.ThreadId, cancellationToken);

        await foreach (var evt in stream.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return evt;
        }
    }
}