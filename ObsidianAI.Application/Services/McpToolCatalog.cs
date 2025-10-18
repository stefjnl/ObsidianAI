using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ObsidianAI.Application.Services;

/// <summary>
/// Default in-memory implementation of <see cref="IMcpToolCatalog"/> that merges MCP tools from
/// the Obsidian vault and Microsoft Learn endpoints while avoiding redundant discovery calls.
/// </summary>
public sealed class McpToolCatalog : IMcpToolCatalog
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(2);

    private readonly IMcpClientProvider _vaultClientProvider;
    private readonly IMicrosoftLearnMcpClientProvider _learnClientProvider;
    private readonly ILogger<McpToolCatalog> _logger;
    private readonly SemaphoreSlim _sync = new(1, 1);

    private IReadOnlyList<object> _cachedTools = Array.Empty<object>();
    private McpToolCatalogSnapshot? _cachedSnapshot;

    /// <summary>
    /// Initializes a new instance of the <see cref="McpToolCatalog"/> class.
    /// </summary>
    public McpToolCatalog(
        IMcpClientProvider vaultClientProvider,
        IMicrosoftLearnMcpClientProvider learnClientProvider,
        ILogger<McpToolCatalog> logger)
    {
        _vaultClientProvider = vaultClientProvider ?? throw new ArgumentNullException(nameof(vaultClientProvider));
        _learnClientProvider = learnClientProvider ?? throw new ArgumentNullException(nameof(learnClientProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<McpToolCatalogSnapshot> GetToolsAsync(CancellationToken ct = default)
    {
    if (_cachedSnapshot is { } snapshot && snapshot.ExpiresAt > DateTimeOffset.UtcNow)
        {
            return snapshot;
        }

        await _sync.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_cachedSnapshot is { } refreshedSnapshot && refreshedSnapshot.ExpiresAt > DateTimeOffset.UtcNow)
            {
                return refreshedSnapshot;
            }

            var mergedTools = new List<object>();
            var vaultToolCount = 0;
            var learnToolCount = 0;

            var vaultClient = await _vaultClientProvider.GetClientAsync(ct).ConfigureAwait(false);
            if (vaultClient is not null)
            {
                try
                {
                    var vaultTools = await vaultClient.ListToolsAsync(cancellationToken: ct).ConfigureAwait(false);
                    vaultToolCount = AppendTools(mergedTools, vaultTools);
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "Failed to list Obsidian MCP tools. Using previously cached set when available.");
                }
            }
            else
            {
                _logger.LogDebug("Obsidian MCP client unavailable when refreshing tool catalog.");
            }

            var learnClient = await _learnClientProvider.GetClientAsync(ct).ConfigureAwait(false);
            if (learnClient is not null)
            {
                try
                {
                    var learnTools = await learnClient.ListToolsAsync(cancellationToken: ct).ConfigureAwait(false);
                    learnToolCount = AppendTools(mergedTools, learnTools);
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "Failed to list Microsoft Learn MCP tools. Using previously cached set when available.");
                }
            }
            else
            {
                _logger.LogDebug("Microsoft Learn MCP client unavailable when refreshing tool catalog.");
            }

            var expiresAt = DateTimeOffset.UtcNow.Add(DefaultTtl);
            _cachedTools = mergedTools.ToArray();
            _cachedSnapshot = new McpToolCatalogSnapshot(_cachedTools, vaultToolCount, learnToolCount, expiresAt);

            _logger.LogInformation("📦 Refreshed MCP tool catalog with {ObsidianCount} Obsidian + {LearnCount} Microsoft Learn tools (expires {ExpiresAt:O})", vaultToolCount, learnToolCount, expiresAt);

            return _cachedSnapshot;
        }
        finally
        {
            _sync.Release();
        }
    }

    /// <inheritdoc />
    public void Invalidate()
    {
        _cachedTools = Array.Empty<object>();
        _cachedSnapshot = null;
    }

    private static int AppendTools(List<object> target, IEnumerable<object> source)
    {
        if (source is null)
        {
            return 0;
        }

        var added = 0;
        foreach (var tool in source)
        {
            if (tool is null)
            {
                continue;
            }

            var toolName = TryGetToolName(tool);
            if (toolName is null)
            {
                continue;
            }

            if (target.Any(existing => string.Equals(TryGetToolName(existing), toolName, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            target.Add(tool);
            added++;
        }

        return added;
    }

    private static string? TryGetToolName(object tool)
    {
        return tool.GetType().GetProperty("Name")?.GetValue(tool) as string;
    }
}
