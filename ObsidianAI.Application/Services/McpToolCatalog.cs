using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ObsidianAI.Application.Services;

/// <summary>
/// Default in-memory implementation of <see cref="IMcpToolCatalog"/> that merges MCP tools from
/// all configured MCP servers while avoiding redundant discovery calls.
/// </summary>
public sealed class McpToolCatalog : IMcpToolCatalog
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);
    private const string CacheKey = "mcp-tools-all";

    private readonly IMcpClientProvider _clientProvider;
    private readonly IMemoryCache _cache;
    private readonly ILogger<McpToolCatalog> _logger;
    private readonly SemaphoreSlim _sync = new(1, 1);

    private IReadOnlyList<object> _cachedTools = Array.Empty<object>();
    private McpToolCatalogSnapshot? _cachedSnapshot;

    /// <summary>
    /// Initializes a new instance of the <see cref="McpToolCatalog"/> class.
    /// </summary>
    public McpToolCatalog(
        IMcpClientProvider clientProvider,
        IMemoryCache cache,
        ILogger<McpToolCatalog> logger)
    {
        _clientProvider = clientProvider ?? throw new ArgumentNullException(nameof(clientProvider));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<McpToolCatalogSnapshot> GetToolsAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue<McpToolCatalogSnapshot>(CacheKey, out var cachedSnapshot) && cachedSnapshot != null)
        {
            _logger.LogDebug("Returning {Count} tools from cache", cachedSnapshot.ObsidianToolCount + cachedSnapshot.MicrosoftLearnToolCount);
            return cachedSnapshot;
        }

        await _sync.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_cache.TryGetValue<McpToolCatalogSnapshot>(CacheKey, out var refreshedSnapshot) && refreshedSnapshot != null)
            {
                return refreshedSnapshot;
            }

            var allTools = await FetchAllToolsAsync(ct);
            
            _cache.Set(CacheKey, _cachedSnapshot, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = DefaultTtl,
                Priority = CacheItemPriority.Normal
            });

            _logger.LogInformation("Cached {Count} tools from {ServerCount} servers", 
                allTools.Count, 
                _clientProvider.GetAvailableServers().Count);

            return _cachedSnapshot!;
        }
        finally
        {
            _sync.Release();
        }
    }

    private async Task<IReadOnlyList<object>> FetchAllToolsAsync(CancellationToken ct)
    {
        var servers = _clientProvider.GetAvailableServers();
        if (servers.Count == 0)
        {
            _logger.LogWarning("No MCP servers available for tool fetching");
            _cachedSnapshot = new McpToolCatalogSnapshot(Array.Empty<object>(), 0, 0, DateTimeOffset.UtcNow.Add(DefaultTtl));
            return Array.Empty<object>();
        }

        _logger.LogInformation("Fetching tools from {Count} MCP servers concurrently", servers.Count);

        var mergedTools = new List<object>();
        var toolCountByServer = new Dictionary<string, int>();

        // Fetch tools from all servers concurrently
        var fetchTasks = servers.Select(async serverName =>
        {
            try
            {
                var tools = await _clientProvider.ListToolsAsync(serverName, ct);
                var toolsList = tools.ToList();
                var addedCount = AppendTools(mergedTools, toolsList);
                
                lock (toolCountByServer)
                {
                    toolCountByServer[serverName] = addedCount;
                }

                _logger.LogDebug("Fetched {Count} tools from {ServerName}", addedCount, serverName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch tools from {ServerName}", serverName);
                lock (toolCountByServer)
                {
                    toolCountByServer[serverName] = 0;
                }
            }
        });

        await Task.WhenAll(fetchTasks);

        var expiresAt = DateTimeOffset.UtcNow.Add(DefaultTtl);
        _cachedTools = mergedTools.ToArray();
        
        // For backward compatibility, use first two servers as vault and learn counts
        var serverNames = toolCountByServer.Keys.ToList();
        var vaultCount = toolCountByServer.GetValueOrDefault(serverNames.ElementAtOrDefault(0) ?? "obsidian", 0);
        var learnCount = toolCountByServer.GetValueOrDefault(serverNames.ElementAtOrDefault(1) ?? "microsoft-learn", 0);
        
        _cachedSnapshot = new McpToolCatalogSnapshot(_cachedTools, vaultCount, learnCount, expiresAt);

        _logger.LogInformation("📦 Refreshed MCP tool catalog with {TotalCount} tools from {ServerCount} servers (expires {ExpiresAt:O})", 
            mergedTools.Count, servers.Count, expiresAt);

        return _cachedTools;
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
    public async Task<IEnumerable<McpTool>> GetToolsFromServersAsync(
        IEnumerable<string> serverNames, 
        CancellationToken cancellationToken = default)
    {
        var serverList = serverNames.ToList();
        if (serverList.Count == 0)
        {
            _logger.LogWarning("No servers specified for tool fetching");
            return Enumerable.Empty<McpTool>();
        }

        _logger.LogInformation("Fetching tools from {Count} servers: {Servers}", 
            serverList.Count, 
            string.Join(", ", serverList));

        var fetchTasks = serverList.Select(async serverName =>
        {
            try
            {
                var tools = await _clientProvider.ListToolsAsync(serverName, cancellationToken);
                var mcpTools = tools.Select(t => new McpTool
                {
                    ServerName = serverName,
                    Tool = t
                }).ToList();

                _logger.LogDebug("Fetched {Count} tools from {ServerName}", 
                    mcpTools.Count, serverName);
                return mcpTools.AsEnumerable();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch tools from {ServerName}", serverName);
                return Enumerable.Empty<McpTool>();
            }
        });

        var results = await Task.WhenAll(fetchTasks);
        var allTools = results.SelectMany(r => r).ToList();

        _logger.LogInformation("Fetched {TotalCount} tools from {ServerCount} servers", 
            allTools.Count, serverList.Count);

        return allTools;
    }
}
