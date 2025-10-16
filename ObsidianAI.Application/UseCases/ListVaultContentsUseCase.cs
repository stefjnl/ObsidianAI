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
    private readonly ILogger<ListVaultContentsUseCase> _logger;

    public ListVaultContentsUseCase(
        IMcpClientProvider mcpClientProvider,
        ILogger<ListVaultContentsUseCase> logger)
    {
        _mcpClientProvider = mcpClientProvider;
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
        try
        {
            var mcpClient = await _mcpClientProvider.GetClientAsync(ct).ConfigureAwait(false);
            if (mcpClient == null)
            {
                _logger.LogWarning("MCP client is not available. Returning empty vault contents.");
                return new VaultContentsResponse(new List<VaultItemDto>(), folderPath ?? "/");
            }

            CallToolResult toolResult;
            string currentPath;

            if (string.IsNullOrWhiteSpace(folderPath))
            {
                // Browse root vault
                _logger.LogInformation("Listing root vault contents");
                toolResult = await mcpClient.CallToolAsync(
                    "obsidian_list_files_in_vault",
                    new Dictionary<string, object?>(),
                    cancellationToken: ct).ConfigureAwait(false);
                currentPath = "/";
            }
            else
            {
                // Browse specific folder
                _logger.LogInformation("Listing contents of folder: {FolderPath}", folderPath);
                toolResult = await mcpClient.CallToolAsync(
                    "obsidian_list_files_in_dir",
                    new Dictionary<string, object?> { ["path"] = folderPath },
                    cancellationToken: ct).ConfigureAwait(false);
                currentPath = folderPath;
            }

            var items = ParseVaultItems(toolResult.Content);
            _logger.LogInformation("Retrieved {Count} vault items from {Path}", items.Count, currentPath);

            return new VaultContentsResponse(items, currentPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list vault contents for path: {FolderPath}", folderPath);
            return new VaultContentsResponse(new List<VaultItemDto>(), folderPath ?? "/");
        }
    }

    private List<VaultItemDto> ParseVaultItems(IEnumerable<ContentBlock>? content)
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

        // Parse line by line
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                continue;
            }

            // Determine if it's a folder (ends with /) or file
            var isFolder = trimmedLine.EndsWith("/", StringComparison.Ordinal);
            var path = isFolder ? trimmedLine.TrimEnd('/') : trimmedLine;
            var name = GetFileName(path);

            if (isFolder)
            {
                items.Add(new VaultItemDto(
                    Name: name,
                    Path: path,
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
                    Path: path,
                    Type: VaultItemType.File,
                    Extension: extension,
                    Size: null, // MCP doesn't provide size in list response
                    LastModified: null)); // MCP doesn't provide timestamp in list response
            }
        }

        // Sort: folders first, then files, both alphabetically
        return items
            .OrderBy(i => i.Type == VaultItemType.File ? 1 : 0)
            .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
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
