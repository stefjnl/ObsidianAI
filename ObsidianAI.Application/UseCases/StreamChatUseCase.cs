namespace ObsidianAI.Application.UseCases;

/// <summary>
/// Use case for streaming chat interactions with an AI agent.
/// </summary>
public class StreamChatUseCase
{
    private readonly ObsidianAI.Domain.Ports.IAIAgentFactory _agentFactory;
    private readonly ObsidianAI.Application.Services.IMcpClientProvider? _mcpClientProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamChatUseCase"/> class.
    /// </summary>
    /// <param name="agentFactory">Factory to create AI agents.</param>
    /// <param name="mcpClient">Optional MCP client for fetching available tools.</param>
    public StreamChatUseCase(
        ObsidianAI.Domain.Ports.IAIAgentFactory agentFactory,
        ObsidianAI.Application.Services.IMcpClientProvider? mcpClientProvider = null)
    {
        _agentFactory = agentFactory;
        _mcpClientProvider = mcpClientProvider;
    }

    /// <summary>
    /// Executes the stream chat use case.
    /// </summary>
    /// <param name="input">The chat input containing the user's message.</param>
    /// <param name="instructions">Instructions for the AI agent.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An asynchronous enumerable of chat stream events.</returns>
    public async IAsyncEnumerable<ObsidianAI.Domain.Models.ChatStreamEvent> ExecuteAsync(
        ObsidianAI.Domain.Models.ChatInput input,
        string instructions,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input.Message))
        {
            throw new ArgumentException("Message cannot be null or whitespace.", nameof(input));
        }

        // Fetch MCP tools if available
        System.Collections.Generic.IEnumerable<object>? tools = null;
        if (_mcpClientProvider != null)
        {
            var mcpClient = await _mcpClientProvider.GetClientAsync(ct).ConfigureAwait(false);
            if (mcpClient != null)
            {
                tools = await mcpClient.ListToolsAsync(cancellationToken: ct).ConfigureAwait(false);
            }
        }

    var agent = await _agentFactory.CreateAgentAsync(instructions, tools, ct).ConfigureAwait(false);

        await foreach (var evt in agent.StreamAsync(input.Message, ct).ConfigureAwait(false))
        {
            yield return evt;
        }
    }
}