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
/// Factory that creates IChatAgent instances for NanoGPT with the currently selected model.
/// </summary>
public class ConfiguredAIAgentFactory : IAIAgentFactory
{
    private readonly IOptions<AppSettings> _options;
    private readonly IConfiguration _configuration;
    private readonly IReadOnlyList<IFunctionMiddleware> _middlewares;
    private readonly ILogger<ConfiguredAIAgentFactory> _logger;
    private readonly ILlmProviderRuntimeStore _runtimeStore;

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
    public string ProviderName => "NanoGPT";

    /// <inheritdoc />
    public string GetModelName() => _runtimeStore.CurrentModel;

    /// <inheritdoc />
    public async Task<IChatAgent> CreateAgentAsync(
        string instructions,
        IEnumerable<object>? tools = null,
        IAgentThreadProvider? threadProvider = null,
        CancellationToken cancellationToken = default)
    {
        // Wrap tools with middleware if provided
        var wrappedTools = tools is not null && _middlewares.Count > 0
            ? tools.OfType<AIFunction>()
                   .WithMiddleware(_middlewares.ToArray())
                   .Cast<object>()
            : tools;

        var model = _runtimeStore.CurrentModel;
        _logger.LogDebug("Creating NanoGPT agent with model {Model} and {ToolCount} tools", model, wrappedTools?.Count() ?? 0);

        return await NanoGptChatAgent.CreateAsync(
            _options,
            _configuration,
            instructions,
            wrappedTools,
            threadProvider,
            model,
            cancellationToken).ConfigureAwait(false);
    }
}