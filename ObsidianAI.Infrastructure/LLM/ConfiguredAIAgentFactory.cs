using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ObsidianAI.Domain.Ports;
using ObsidianAI.Infrastructure.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ObsidianAI.Infrastructure.LLM;

/// <summary>
/// Provider-agnostic factory that creates IChatAgent instances based on the runtime provider selection.
/// </summary>
public class ConfiguredAIAgentFactory : IAIAgentFactory
{
    private readonly IOptions<AppSettings> _options;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfiguredAIAgentFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfiguredAIAgentFactory"/> class.
    /// </summary>
    /// <param name="options">The application settings options.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="logger">The logger instance.</param>
    public ConfiguredAIAgentFactory(
        IOptions<AppSettings> options,
        IConfiguration configuration,
        ILogger<ConfiguredAIAgentFactory> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string ProviderName => _options.Value.LLM.Provider ?? "LMStudio";

    /// <inheritdoc />
    public string GetModelName()
    {
        var provider = ProviderName;
        return provider switch
        {
            var name when name.Equals("LMStudio", StringComparison.OrdinalIgnoreCase) => _options.Value.LLM.LMStudio?.Model ?? "openai/gpt-oss-20b",
            var name when name.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase) => _options.Value.LLM.OpenRouter?.Model ?? "anthropic/claude-3-haiku",
            var name when name.Equals("NanoGPT", StringComparison.OrdinalIgnoreCase) => _options.Value.LLM.NanoGPT?.Model ?? "nano-gpt",
            _ => "unknown"
        };
    }

    /// <inheritdoc />
    public async Task<IChatAgent> CreateAgentAsync(
        string instructions,
        System.Collections.Generic.IEnumerable<object>? tools = null,
        IAgentThreadProvider? threadProvider = null,
        CancellationToken cancellationToken = default)
    {
        var provider = ProviderName;
        _logger.LogDebug("Creating chat agent for provider {Provider} with {ToolCount} tools", provider, tools?.Count() ?? 0);

        return provider switch
        {
            var name when name.Equals("LMStudio", StringComparison.OrdinalIgnoreCase)
                => await LmStudioChatAgent.CreateAsync(_options, instructions, tools, threadProvider, cancellationToken).ConfigureAwait(false),
            var name when name.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase)
                => await OpenRouterChatAgent.CreateAsync(_options, _configuration, instructions, tools, threadProvider, cancellationToken).ConfigureAwait(false),
            var name when name.Equals("NanoGPT", StringComparison.OrdinalIgnoreCase)
                => await NanoGptChatAgent.CreateAsync(_options, _configuration, instructions, tools, threadProvider, cancellationToken).ConfigureAwait(false),
            _ => throw new InvalidOperationException($"Unsupported LLM provider '{provider}'.")
        };
    }
}