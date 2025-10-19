using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ObsidianAI.Domain.Ports;
using ObsidianAI.Infrastructure.Configuration;
using ObsidianAI.Infrastructure.Middleware;
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
    private readonly IReadOnlyList<IFunctionMiddleware> _middlewares;
    private readonly ILogger<ConfiguredAIAgentFactory> _logger;
    private readonly ILlmProviderRuntimeStore _runtimeStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfiguredAIAgentFactory"/> class.
    /// </summary>
    /// <param name="options">The application settings options.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="middlewares">The function middlewares to wrap around tools.</param>
    /// <param name="logger">The logger instance.</param>
    public ConfiguredAIAgentFactory(
        IOptions<AppSettings> options,
        IConfiguration configuration,
        IEnumerable<IFunctionMiddleware> middlewares,
        ILogger<ConfiguredAIAgentFactory> logger,
        ILlmProviderRuntimeStore runtimeStore)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _middlewares = middlewares?.ToList() ?? [];
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runtimeStore = runtimeStore ?? throw new ArgumentNullException(nameof(runtimeStore));

        if (_middlewares.Count > 0)
        {
            _logger.LogInformation(
                "Factory initialized with {MiddlewareCount} tool middlewares: {MiddlewareTypes}",
                _middlewares.Count,
                string.Join(", ", _middlewares.Select(m => m.GetType().Name)));
        }
    }

    /// <inheritdoc />
    public string ProviderName => _runtimeStore.CurrentProvider;

    /// <inheritdoc />
    public string GetModelName()
    {
        return _runtimeStore.CurrentModel;
    }

    /// <inheritdoc />
    public async Task<IChatAgent> CreateAgentAsync(
        string instructions,
        System.Collections.Generic.IEnumerable<object>? tools = null,
        IAgentThreadProvider? threadProvider = null,
        CancellationToken cancellationToken = default)
    {
        // Wrap tools with middleware if provided
        var wrappedTools = tools is not null && _middlewares.Count > 0
            ? tools.OfType<AIFunction>()
                   .WithMiddleware(_middlewares.ToArray())
                   .Cast<object>()
            : tools;
        var provider = ProviderName;
        _logger.LogDebug("Creating chat agent for provider {Provider} with {ToolCount} tools", provider, wrappedTools?.Count() ?? 0);

        return provider switch
        {
            var name when name.Equals("LMStudio", StringComparison.OrdinalIgnoreCase)
                => await LmStudioChatAgent.CreateAsync(_options, instructions, wrappedTools, threadProvider, cancellationToken).ConfigureAwait(false),
            var name when name.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase)
                => await OpenRouterChatAgent.CreateAsync(_options, _configuration, instructions, wrappedTools, threadProvider, cancellationToken).ConfigureAwait(false),
            var name when name.Equals("NanoGPT", StringComparison.OrdinalIgnoreCase)
                => await NanoGptChatAgent.CreateAsync(_options, _configuration, instructions, wrappedTools, threadProvider, cancellationToken).ConfigureAwait(false),
            _ => throw new InvalidOperationException($"Unsupported LLM provider '{provider}'.")
        };
    }
}