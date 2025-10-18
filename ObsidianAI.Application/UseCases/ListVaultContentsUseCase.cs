using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ObsidianAI.Application.Contracts;
using ObsidianAI.Application.Services;

namespace ObsidianAI.Application.UseCases;

/// <summary>
/// Use case for listing vault contents (files and folders).
/// </summary>
public sealed class ListVaultContentsUseCase
{
    private readonly IMcpClientProvider _mcpClientProvider;
    private readonly IVaultIndexCache _vaultIndexCache;
    private readonly ILogger<ListVaultContentsUseCase> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(10);

    public ListVaultContentsUseCase(
        IMcpClientProvider mcpClientProvider,
        IVaultIndexCache vaultIndexCache,
        ILogger<ListVaultContentsUseCase> logger)
    {
        _mcpClientProvider = mcpClientProvider;
        _vaultIndexCache = vaultIndexCache ?? throw new ArgumentNullException(nameof(vaultIndexCache));
        _logger = logger;
    }

    /// <summary>
    /// Executes the list vault contents use case.
    /// </summary>
    /// <param name="folderPath">Optional folder path to browse. If null, browses root.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The vault contents response.</returns>
    public async Task<VaultContentsResponse> ExecuteAsync(string? folderPath, CancellationToken ct = default)
    {
        var cacheKey = string.IsNullOrWhiteSpace(folderPath) ? VaultIndexCacheKeys.Root : folderPath!;
        var currentPath = string.IsNullOrWhiteSpace(folderPath) ? "/" : folderPath!;
        VaultIndexEntry? cachedEntry = null;

        if (_vaultIndexCache.TryGet(cacheKey, out var entry))
        {
            cachedEntry = entry;
            if (entry.Items.Count > 0)
            {
                _logger.LogDebug("Serving vault listing for {Path} from cache", currentPath);
                return new VaultContentsResponse(CloneItems(entry.Items), currentPath);
            }
        }

        try
        {
            var mcpClient = await _mcpClientProvider.GetClientAsync(ct).ConfigureAwait(false);
            if (mcpClient == null)
            {
                if (cachedEntry is not null && cachedEntry.Paths.Count > 0)
                {
                    _logger.LogDebug("MCP client unavailable; returning cached vault paths for {Path}.", currentPath);
                    var fallbackItems = BuildItemsFromCachedPaths(cachedEntry.Paths, folderPath);
                    return new VaultContentsResponse(fallbackItems, currentPath);
                }

                _logger.LogWarning("MCP client is not available. Returning empty vault contents.");
                return new VaultContentsResponse(new List<VaultItemDto>(), currentPath);
            }

            CallToolResult toolResult;

            if (string.IsNullOrWhiteSpace(folderPath))
            {
                // Browse root vault
                _logger.LogInformation("Listing root vault contents");
                toolResult = await mcpClient.CallToolAsync(
                    "obsidian_list_files_in_vault",
                    new Dictionary<string, object?>(),
                    cancellationToken: ct).ConfigureAwait(false);
            }
            else
            {
                // Browse specific folder
                _logger.LogInformation("Listing contents of folder: {FolderPath}", folderPath);
                toolResult = await mcpClient.CallToolAsync(
                    "obsidian_list_files_in_dir",
                    new Dictionary<string, object?> { ["dirpath"] = folderPath },
                    cancellationToken: ct).ConfigureAwait(false);
            }

            var contentBlocks = toolResult.Content?.ToList();
            var items = ParseVaultItems(contentBlocks, folderPath);
            _logger.LogInformation("Retrieved {Count} vault items from {Path}", items.Count, currentPath);

            // Debug: Log first few items
            foreach (var item in items.Take(5))
            {
                _logger.LogInformation("Parsed item - Name: '{Name}', Path: '{Path}', Type: {Type}",
                    item.Name, item.Path, item.Type);
            }

            var rawPaths = VaultToolResponseParser.ExtractPaths(contentBlocks);
            var storedPaths = rawPaths.Count > 0
                ? rawPaths
                : items.Select(i => i.Type == VaultItemType.Folder ? i.Path.TrimEnd('/') + "/" : i.Path).ToList();

            _vaultIndexCache.Set(cacheKey, storedPaths, items, CacheDuration);

            return new VaultContentsResponse(items, currentPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list vault contents for path: {FolderPath}", folderPath);
            return new VaultContentsResponse(new List<VaultItemDto>(), currentPath);
        }
    }

    private static List<VaultItemDto> CloneItems(IReadOnlyList<VaultItemDto> source)
    {
        if (source.Count == 0)
        {
            return new List<VaultItemDto>();
        }

        return source.Select(item => item with { }).ToList();
    }

    private List<VaultItemDto> BuildItemsFromCachedPaths(IReadOnlyList<string> paths, string? parentPath)
    {
        if (paths.Count == 0)
        {
            return new List<VaultItemDto>();
        }

        var synthesized = string.Join(Environment.NewLine, paths);
        return ParseAsLines(synthesized, parentPath);
    }

    private List<VaultItemDto> ParseVaultItems(IEnumerable<ContentBlock>? content, string? parentPath)
    {
        if (content == null)
        {
            return new List<VaultItemDto>();
        }

        var textBlock = content.OfType<TextContentBlock>().FirstOrDefault();
        if (textBlock == null || string.IsNullOrWhiteSpace(textBlock.Text))
        {
            return new List<VaultItemDto>();
        }

        var text = textBlock.Text.Trim();
        var items = new List<VaultItemDto>();

        // Debug: Log raw response
        _logger.LogInformation("MCP raw response (first 500 chars): {Text}",
            text.Length > 500 ? text.Substring(0, 500) : text);

        // Check if the response contains an MCP error
        if (text.Contains("Caught Exception. Error: Error 40400: Not Found", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Caught Exception", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("MCP returned an error response: {ErrorText}", text);
            return new List<VaultItemDto>();
        }

        // Check if response is JSON array format
        if (text.StartsWith("[") && text.EndsWith("]"))
        {
            try
            {
                // Parse as JSON array
                var paths = System.Text.Json.JsonSerializer.Deserialize<List<string>>(text);
                if (paths != null)
                {
                    foreach (var path in paths)
                    {
                        if (string.IsNullOrWhiteSpace(path))
                        {
                            continue;
                        }

                        // Determine if it's a folder (ends with /) or file
                        var isFolder = path.EndsWith("/", StringComparison.Ordinal);
                        var cleanPath = isFolder ? path.TrimEnd('/') : path;

                        // Build full path: if we're browsing a subfolder, prepend the parent path
                        var fullPath = BuildFullPath(parentPath, cleanPath);
                        var name = GetFileName(cleanPath);

                        if (isFolder)
                        {
                            items.Add(new VaultItemDto(
                                Name: name,
                                Path: fullPath,
                                Type: VaultItemType.Folder,
                                Extension: null,
                                Size: null,
                                LastModified: null));
                        }
                        else
                        {
                            var extension = System.IO.Path.GetExtension(path);
                            items.Add(new VaultItemDto(
                                Name: name,
                                Path: fullPath,
                                Type: VaultItemType.File,
                                Extension: extension,
                                Size: null,
                                LastModified: null));
                        }
                    }
                }
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse MCP response as JSON, falling back to line-by-line parsing");
                // Check if the response contains an error message in the text
                if (text.Contains("Caught Exception", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("MCP response contains error message, returning empty list");
                    return new List<VaultItemDto>();
                }
                // Fall back to line-by-line parsing
                return ParseAsLines(text, parentPath);
            }
        }
        else
        {
            // Parse line by line for non-JSON responses
            return ParseAsLines(text, parentPath);
        }

        // Sort: folders first, then files, both alphabetically
        return items
            .OrderBy(i => i.Type == VaultItemType.File ? 1 : 0)
            .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private List<VaultItemDto> ParseAsLines(string text, string? parentPath)
    {
        var items = new List<VaultItemDto>();
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                continue;
            }

            // Skip error messages that might be in the response
            if (trimmedLine.Contains("Caught Exception", StringComparison.OrdinalIgnoreCase) ||
                trimmedLine.Contains("Error:", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Skipping error line in MCP response: {Line}", trimmedLine);
                continue;
            }

            // Determine if it's a folder (ends with /) or file
            var isFolder = trimmedLine.EndsWith("/", StringComparison.Ordinal);
            var path = isFolder ? trimmedLine.TrimEnd('/') : trimmedLine;

            // Build full path: if we're browsing a subfolder, prepend the parent path
            var fullPath = BuildFullPath(parentPath, path);
            var name = GetFileName(path);

            if (isFolder)
            {
                items.Add(new VaultItemDto(
                    Name: name,
                    Path: fullPath,
                    Type: VaultItemType.Folder,
                    Extension: null,
                    Size: null,
                    LastModified: null));
            }
            else
            {
                var extension = System.IO.Path.GetExtension(path);
                items.Add(new VaultItemDto(
                    Name: name,
                    Path: fullPath,
                    Type: VaultItemType.File,
                    Extension: extension,
                    Size: null,
                    LastModified: null));
            }
        }

        return items;
    }

    private static string BuildFullPath(string? parentPath, string itemPath)
    {
        // If no parent path, return the item path as-is
        if (string.IsNullOrWhiteSpace(parentPath))
        {
            return itemPath;
        }

        // If itemPath already contains parent path, return as-is
        if (itemPath.StartsWith(parentPath, StringComparison.OrdinalIgnoreCase))
        {
            return itemPath;
        }

        // Combine parent path with item path using forward slash
        return $"{parentPath.TrimEnd('/')}/{itemPath.TrimStart('/')}";
    }

    private static string GetFileName(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        var lastSlash = path.LastIndexOf('/');
        return lastSlash >= 0 ? path.Substring(lastSlash + 1) : path;
    }
}
