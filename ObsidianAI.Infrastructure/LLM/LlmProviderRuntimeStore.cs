using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ObsidianAI.Domain.Ports;
using ObsidianAI.Infrastructure.Configuration;

namespace ObsidianAI.Infrastructure.LLM;

/// <summary>
/// In-memory store that tracks the active LLM model, allowing runtime switching without restarts.
/// NanoGPT is the sole provider; only model switching is supported.
/// </summary>
public sealed class LlmProviderRuntimeStore : ILlmProviderRuntimeStore
{
    private readonly IOptionsMonitor<AppSettings> _appSettings;
    private readonly ILogger<LlmProviderRuntimeStore> _logger;
    private readonly object _sync = new();

    private string _currentModel;

    public LlmProviderRuntimeStore(IOptionsMonitor<AppSettings> appSettings, ILogger<LlmProviderRuntimeStore> logger)
    {
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var llmSettings = _appSettings.CurrentValue.LLM ?? throw new InvalidOperationException("LLM configuration section is missing.");
        var nanoGpt = llmSettings.NanoGPT ?? throw new InvalidOperationException("NanoGPT configuration section is missing.");
        _currentModel = nanoGpt.DefaultModel;
    }

    /// <inheritdoc />
    public string CurrentProvider => "NanoGPT";

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
    public IReadOnlyList<ModelInfo> GetAvailableModels()
    {
        var nanoGpt = _appSettings.CurrentValue.LLM?.NanoGPT;
        if (nanoGpt?.Models is null || nanoGpt.Models.Length == 0)
        {
            return new List<ModelInfo>();
        }

        return nanoGpt.Models
            .Select(m => new ModelInfo(m.Name, m.Identifier))
            .ToList();
    }

    /// <inheritdoc />
    public bool TrySwitchModel(string modelIdentifier, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(modelIdentifier))
        {
            error = "Model identifier is required.";
            return false;
        }

        var nanoGpt = _appSettings.CurrentValue.LLM?.NanoGPT;
        if (nanoGpt is null)
        {
            error = "NanoGPT configuration is unavailable.";
            return false;
        }

        if (!nanoGpt.IsValidModelIdentifier(modelIdentifier))
        {
            error = $"Model '{modelIdentifier}' is not available. Valid models: {string.Join(", ", nanoGpt.Models.Select(m => m.Identifier))}";
            return false;
        }

        lock (_sync)
        {
            if (_currentModel == modelIdentifier)
            {
                return true;
            }

            _currentModel = modelIdentifier;
        }

        _logger.LogInformation("Switched to model {Model}", modelIdentifier);
        return true;
    }

    /// <inheritdoc />
    public bool TrySwitchProvider(string providerName, out string model, out string? error)
    {
        // Legacy method - NanoGPT is the only provider
        // Treat provider switch as a no-op (always NanoGPT)
        model = CurrentModel;
        error = null;

        if (!string.Equals(providerName, "NanoGPT", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Provider '{Provider}' requested but NanoGPT is the only provider. Using NanoGPT.", providerName);
        }

        return true;
    }
}
