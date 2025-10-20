using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Agents.AI;
using ObsidianAI.Domain.Models;
using ObsidianAI.Domain.Ports;
using ObsidianAI.Infrastructure.Configuration;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using DomainChatResponse = global::ObsidianAI.Domain.Models.ChatResponse;

namespace ObsidianAI.Infrastructure.LLM;

/// <summary>
/// IChatAgent and IAIClient implementation for NanoGPT deployments using the OpenAI-compatible pipeline.
/// </summary>
public sealed class NanoGptChatAgent : BaseChatAgent, IChatAgent, IAIClient
{
    /// <summary>
    /// Constructor for DI. For factory-based creation with tools, use CreateAsync.
    /// </summary>
    public NanoGptChatAgent(
        IOptions<AppSettings> appOptions,
        IConfiguration configuration)
        : this(appOptions, configuration, string.Empty, null, null)
    {
    }

    /// <summary>
    /// Private constructor used by factory method and DI constructor.
    /// </summary>
    private NanoGptChatAgent(
        IOptions<AppSettings> appOptions,
        IConfiguration configuration,
        string instructions,
        IEnumerable<object>? tools,
        IAgentThreadProvider? threadProvider)
        : base(
            CreateChatClient(appOptions, configuration),
            "NanoGptAgent",
            instructions,
            tools,
            threadProvider,
            (appOptions.Value.LLM.NanoGPT ?? new NanoGptSettings()).Model ?? "nanogpt-model")
    {
    }

    private static IChatClient CreateChatClient(IOptions<AppSettings> appOptions, IConfiguration configuration)
    {
        var nanoGptSettings = appOptions.Value.LLM.NanoGPT ?? new NanoGptSettings();
        var endpoint = nanoGptSettings.Endpoint?.Trim();
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new InvalidOperationException("NanoGPT endpoint is not configured.");
        }

        var apiKey = configuration["NanoGpt:ApiKey"] ?? nanoGptSettings.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("NanoGPT API key is not configured. Set NanoGpt:ApiKey via user secrets or environment variables.");
        }

        var model = nanoGptSettings.Model;
        var client = new OpenAI.OpenAIClient(
            new System.ClientModel.ApiKeyCredential(apiKey),
            new OpenAI.OpenAIClientOptions { Endpoint = new Uri(endpoint) });

        return client.GetChatClient(model).AsIChatClient();
    }

    public static Task<NanoGptChatAgent> CreateAsync(
        IOptions<AppSettings> appOptions,
        IConfiguration configuration,
        string instructions,
        IEnumerable<object>? tools = null,
        IAgentThreadProvider? threadProvider = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new NanoGptChatAgent(appOptions, configuration, instructions, tools, threadProvider));
    }

    // ========================================================================
    // IChatAgent Implementation (delegates to BaseChatAgent core methods)
    // ========================================================================

    public Task<DomainChatResponse> SendAsync(string message, string? threadId = null, CancellationToken ct = default)
        => SendAsyncCore(message, threadId, ct);

    public IAsyncEnumerable<ChatStreamEvent> StreamAsync(string message, string? threadId = null, CancellationToken ct = default)
        => StreamAsyncCore(message, threadId, ct);

    public Task<AgentThread> CreateThreadAsync(CancellationToken ct = default)
        => CreateThreadAsyncCore(ct);

    // ========================================================================
    // IAIClient Implementation (delegates to BaseChatAgent core methods)
    // ========================================================================

    public string ProviderName => "NanoGPT";

    public Task<AIResponse> CallAsync(AIRequest request, CancellationToken cancellationToken = default)
        => CallAsyncCore(request, ProviderName, cancellationToken);

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

    public Task<IEnumerable<string>> GetModelsAsync(CancellationToken cancellationToken = default)
        => GetModelsCoreAsync(cancellationToken);
}
