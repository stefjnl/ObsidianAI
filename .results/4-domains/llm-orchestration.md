# LLM Orchestration Deep Dive

LLM interactions span the API and Infrastructure layers, centering on the Microsoft Agent Framework. The orchestration pipeline abstracts provider differences (LM Studio vs. OpenRouter) while keeping conversation context and MCP tool access intact.

## Key Conventions
- **Provider-agnostic factories:** `ConfiguredAIAgentFactory` inspects `AppSettings.LLM.Provider` and delegates to provider-specific agent builders.
- **Microsoft Agent Framework adapters:** Infrastructure factories convert provider SDK clients into `IChatClient` instances via `.AsIChatClient()`, enabling shared agent APIs.
- **Thread continuity:** Conversations store `ThreadId` values returned by the agent and reuse them through `IAgentThreadProvider` to preserve context.
- **Tool wiring:** MCP tool discovery (`IMcpClientProvider.ListToolsAsync`) feeds directly into agent creation so the assistant can invoke vault operations.

## Representative Code
### Provider-aware agent factory
```csharp
public async Task<IChatAgent> CreateAgentAsync(
    string instructions,
    System.Collections.Generic.IEnumerable<object>? tools = null,
    IAgentThreadProvider? threadProvider = null,
    CancellationToken cancellationToken = default)
{
    return ProviderName.Equals("LMStudio", StringComparison.OrdinalIgnoreCase)
        ? await LmStudioChatAgent.CreateAsync(_options, instructions, tools, threadProvider, cancellationToken).ConfigureAwait(false)
        : ProviderName.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase)
            ? await OpenRouterChatAgent.CreateAsync(_options, instructions, tools, threadProvider, cancellationToken).ConfigureAwait(false)
            : await LmStudioChatAgent.CreateAsync(_options, instructions, tools, threadProvider, cancellationToken).ConfigureAwait(false);
}
```

### Agent initialization inside the API service
```csharp
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
```

## Implementation Notes
- **Factory layering:** API-level `ILlmClientFactory` produces Microsoft Agent `IChatClient` instances (e.g., `LmStudioClientFactory`), while Infrastructure-level `IAIAgentFactory` returns higher-level `IChatAgent` abstractions.
- **Instruction presets:** `AgentInstructions.ObsidianAssistant` contains the canonical system prompt used by both `/chat` and `/chat/stream` endpoints.
- **Logging:** Agent services log provider/model pairing at startup (`Program.cs`) and warn when MCP tooling is unavailable, assisting diagnostics.
- **Thread storage:** `StartChatUseCase.EnsureThreadAsync` writes the agent thread identifier onto the `Conversation` aggregate; future prompts reuse it rather than creating new threads.
- **Tool fallback:** If MCP tooling is offline, the assistant still initializes with zero tools but emits warnings, preventing hard failures in the chat pipeline.
