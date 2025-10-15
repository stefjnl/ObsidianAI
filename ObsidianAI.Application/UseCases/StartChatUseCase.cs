namespace ObsidianAI.Application.UseCases;

/// <summary>
/// Use case for starting a chat interaction with an AI agent and extracting any file operations.
/// </summary>
public class StartChatUseCase
{
    private readonly ObsidianAI.Domain.Ports.IAIAgentFactory _agentFactory;
    private readonly ObsidianAI.Domain.Services.IFileOperationExtractor _extractor;
    private readonly ObsidianAI.Application.Services.IMcpClientProvider? _mcpClientProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartChatUseCase"/> class.
    /// </summary>
    /// <param name="agentFactory">Factory to create AI agents.</param>
    /// <param name="extractor">Extractor for file operations from response text.</param>
    /// <param name="mcpClient">Optional MCP client for fetching available tools.</param>
    public StartChatUseCase(
        ObsidianAI.Domain.Ports.IAIAgentFactory agentFactory,
        ObsidianAI.Domain.Services.IFileOperationExtractor extractor,
        ObsidianAI.Application.Services.IMcpClientProvider? mcpClientProvider = null)
    {
        _agentFactory = agentFactory;
        _extractor = extractor;
        _mcpClientProvider = mcpClientProvider;
    }

    /// <summary>
    /// Executes the start chat use case.
    /// </summary>
    /// <param name="input">The chat input containing the user's message.</param>
    /// <param name="instructions">Instructions for the AI agent.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result containing response text and optional file operation.</returns>
    public async Task<Contracts.StartChatResult> ExecuteAsync(ObsidianAI.Domain.Models.ChatInput input, string instructions, CancellationToken ct = default)
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
        var responseText = await agent.SendAsync(input.Message, ct).ConfigureAwait(false);
        var fileOperation = _extractor.Extract(responseText);

        return new Contracts.StartChatResult(responseText, fileOperation);
    }
}