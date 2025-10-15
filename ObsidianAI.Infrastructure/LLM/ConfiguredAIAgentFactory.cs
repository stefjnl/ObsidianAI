using Microsoft.Extensions.Options;
using ObsidianAI.Domain.Ports;
using ObsidianAI.Infrastructure.Configuration;
using System.Threading;

namespace ObsidianAI.Infrastructure.LLM;

/// <summary>
/// Provider-agnostic factory that creates IChatAgent instances based on AppSettings.LLM.Provider.
/// </summary>
public class ConfiguredAIAgentFactory : IAIAgentFactory
{
    private readonly IOptions<AppSettings> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfiguredAIAgentFactory"/> class.
    /// </summary>
    /// <param name="options">The application settings options.</param>
    public ConfiguredAIAgentFactory(IOptions<AppSettings> options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
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
    public async Task<IChatAgent> CreateAgentAsync(string instructions, System.Collections.Generic.IEnumerable<object>? tools = null, CancellationToken cancellationToken = default)
    {
        return ProviderName.Equals("LMStudio", StringComparison.OrdinalIgnoreCase)
            ? await LmStudioChatAgent.CreateAsync(_options, instructions, tools, cancellationToken).ConfigureAwait(false)
            : ProviderName.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase)
                ? await OpenRouterChatAgent.CreateAsync(_options, instructions, tools, cancellationToken).ConfigureAwait(false)
                : await LmStudioChatAgent.CreateAsync(_options, instructions, tools, cancellationToken).ConfigureAwait(false);
    }
}