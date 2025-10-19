using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ObsidianAI.Domain.Ports;
using ObsidianAI.Infrastructure.Configuration;

namespace ObsidianAI.Infrastructure.LLM;

/// <summary>
/// In-memory store that tracks the active LLM provider and model, allowing runtime switching without restarts.
/// </summary>
public sealed class LlmProviderRuntimeStore : ILlmProviderRuntimeStore
{
    private readonly IOptionsMonitor<AppSettings> _appSettings;
    private readonly ILogger<LlmProviderRuntimeStore> _logger;
    private readonly object _sync = new();

    private string _currentProvider;
    private string _currentModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="LlmProviderRuntimeStore"/> class.
    /// </summary>
    public LlmProviderRuntimeStore(IOptionsMonitor<AppSettings> appSettings, ILogger<LlmProviderRuntimeStore> logger)
    {
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var llmSettings = _appSettings.CurrentValue.LLM ?? throw new InvalidOperationException("LLM configuration section is missing.");
        _currentProvider = NormalizeProviderName(llmSettings.Provider) ?? "LMStudio";
        _currentModel = ResolveModel(llmSettings, _currentProvider) ?? string.Empty;
    }

    /// <inheritdoc />
    public string CurrentProvider
    {
        get
        {
            lock (_sync)
            {
                return _currentProvider;
            }
        }
    }

    /// <inheritdoc />
    public string CurrentModel
    {
        get
        {
            lock (_sync)
            {
                return _currentModel;
            }
        }
    }

    /// <inheritdoc />
    public bool TrySwitchProvider(string providerName, out string model, out string? error)
    {
        model = string.Empty;
        error = null;

        if (string.IsNullOrWhiteSpace(providerName))
        {
            error = "Provider name is required.";
            return false;
        }

        var normalized = NormalizeProviderName(providerName);
        if (normalized is null)
        {
            error = $"Provider '{providerName}' is not supported.";
            return false;
        }

        var llmSettings = _appSettings.CurrentValue.LLM;
        if (llmSettings is null)
        {
            error = "LLM configuration is unavailable.";
            return false;
        }

        var resolvedModel = ResolveModel(llmSettings, normalized);
        if (string.IsNullOrWhiteSpace(resolvedModel))
        {
            error = $"No model configured for provider '{normalized}'.";
            return false;
        }

        lock (_sync)
        {
            if (string.Equals(_currentProvider, normalized, StringComparison.OrdinalIgnoreCase))
            {
                model = _currentModel;
                return true;
            }

            _currentProvider = normalized;
            _currentModel = resolvedModel;
        }

        model = resolvedModel;
        _logger.LogInformation("Switched to {Provider}/{Model}", normalized, resolvedModel);
        return true;
    }

    private static string? NormalizeProviderName(string? providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            return null;
        }

        return providerName.Trim() switch
        {
            var value when value.Equals("LMStudio", StringComparison.OrdinalIgnoreCase) => "LMStudio",
            var value when value.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase) => "OpenRouter",
            var value when value.Equals("NanoGPT", StringComparison.OrdinalIgnoreCase) => "NanoGPT",
            _ => null
        };
    }

    private static string? ResolveModel(LlmSettings settings, string provider)
    {
        if (settings is null)
        {
            return null;
        }

        return provider switch
        {
            "LMStudio" => settings.LMStudio?.Model,
            "OpenRouter" => settings.OpenRouter?.Model,
            "NanoGPT" => settings.NanoGPT?.Model,
            _ => null
        };
    }
}
