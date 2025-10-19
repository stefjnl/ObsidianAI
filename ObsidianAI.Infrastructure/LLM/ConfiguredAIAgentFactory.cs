using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using ObsidianAI.Domain.Ports;
using ObsidianAI.Infrastructure.Configuration;
using ObsidianAI.Infrastructure.Middleware;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ObsidianAI.Infrastructure.LLM;

/// <summary>
/// Provider-agnostic factory that creates IChatAgent instances based on AppSettings.LLM.Provider.
/// </summary>
public class ConfiguredAIAgentFactory : IAIAgentFactory
{
    private readonly IOptions<AppSettings> _options;
    private readonly IConfiguration _configuration;
    private readonly IReadOnlyList<IFunctionMiddleware> _middlewares;
    private readonly ILogger<ConfiguredAIAgentFactory> _logger;

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
        ILogger<ConfiguredAIAgentFactory> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _middlewares = middlewares?.ToList() ?? [];
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogWarning(
            "🔧 Factory initialized with {MiddlewareCount} middlewares: {MiddlewareTypes}",
            _middlewares.Count,
            string.Join(", ", _middlewares.Select(m => m.GetType().Name))
        );
    }

    /// <inheritdoc />
    public string ProviderName => _options.Value.LLM.Provider?.Trim() ?? "LMStudio";

    /// <inheritdoc />
    public string GetModelName()
    {
        return ProviderName.Equals("LMStudio", StringComparison.OrdinalIgnoreCase)
            ? _options.Value.LLM.LMStudio.Model
            : _options.Value.LLM.OpenRouter.Model;
    }

    /// <inheritdoc />
    public async Task<IChatAgent> CreateAgentAsync(
        string instructions,
        System.Collections.Generic.IEnumerable<object>? tools = null,
        IAgentThreadProvider? threadProvider = null,
        CancellationToken cancellationToken = default)
    {
        // LOG: What did we receive?
        Console.WriteLine($"[FACTORY] 📦 CreateAgentAsync received {tools?.Count() ?? 0} tools");
        _logger.LogWarning(
            "📦 CreateAgentAsync received {ToolCount} tools",
            tools?.Count() ?? 0
        );

        if (tools?.Any() == true)
        {
            var firstTool = tools.First();
            Console.WriteLine($"[FACTORY] 📦 First tool type: {firstTool.GetType().FullName}");
            _logger.LogWarning(
                "📦 First tool type: {ToolType}",
                firstTool.GetType().FullName
            );
        }

        // LOG: How many are AIFunction?
        var aiFunctions = tools?.OfType<AIFunction>().ToList() ?? [];
        Console.WriteLine($"[FACTORY] 📦 Filtered to {aiFunctions.Count} AIFunction objects");
        _logger.LogWarning(
            "📦 Filtered to {AIFunctionCount} AIFunction objects",
            aiFunctions.Count
        );

        Console.WriteLine($"[FACTORY] 🔄 About to wrap with {_middlewares.Count} middlewares");
        Console.WriteLine($"[FACTORY] 🔄 Condition check: tools={tools is not null}, middlewares={_middlewares.Count > 0}");

        // Wrap tools with middleware if provided
        var wrappedTools = tools is not null && _middlewares.Count > 0
            ? tools.OfType<AIFunction>()
                   .WithMiddleware(_middlewares.ToArray())
                   .Cast<object>()
            : tools;

        // LOG: What happened during wrapping?
        var wrappedCount = (wrappedTools as IEnumerable<object>)?.Count() ?? 0;
        Console.WriteLine($"[FACTORY] 🔄 Wrapping result: Input={tools?.Count() ?? 0}, Middlewares={_middlewares.Count}, Output={wrappedCount}");
        _logger.LogWarning(
            "🔄 Wrapping result: Input={InputCount}, Middlewares={MiddlewareCount}, Output={OutputCount}",
            tools?.Count() ?? 0,
            _middlewares.Count,
            wrappedCount
        );

        if (wrappedTools is IEnumerable<object> wrapped && wrapped.Any())
        {
            var firstWrapped = wrapped.First();
            Console.WriteLine($"[FACTORY] 🔄 First wrapped tool type: {firstWrapped.GetType().FullName}");
            _logger.LogWarning(
                "🔄 First wrapped tool type: {WrappedType}",
                firstWrapped.GetType().FullName
            );
        }

        return ProviderName.Equals("LMStudio", StringComparison.OrdinalIgnoreCase)
            ? await LmStudioChatAgent.CreateAsync(_options, instructions, wrappedTools, threadProvider, cancellationToken).ConfigureAwait(false)
            : ProviderName.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase)
                ? await OpenRouterChatAgent.CreateAsync(_options, _configuration, instructions, wrappedTools, threadProvider, cancellationToken).ConfigureAwait(false)
                : await LmStudioChatAgent.CreateAsync(_options, instructions, wrappedTools, threadProvider, cancellationToken).ConfigureAwait(false);
    }
}