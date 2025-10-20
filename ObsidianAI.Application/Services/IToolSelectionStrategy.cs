using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ObsidianAI.Application.Services;

public interface IToolSelectionStrategy
{
    Task<IEnumerable<string>> SelectServersForQueryAsync(
        string query,
        CancellationToken cancellationToken = default);
}

public class KeywordBasedToolSelectionStrategy : IToolSelectionStrategy
{
    private const int MaxToolsPerRequest = 20; // OpenRouter limit
    private readonly ILogger<KeywordBasedToolSelectionStrategy> _logger;
    private readonly IMcpToolCatalog _toolCatalog;

    public KeywordBasedToolSelectionStrategy(
        ILogger<KeywordBasedToolSelectionStrategy> logger,
        IMcpToolCatalog toolCatalog)
    {
        _logger = logger;
        _toolCatalog = toolCatalog;
    }

    public async Task<IEnumerable<string>> SelectServersForQueryAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var candidates = new List<string> { "obsidian" }; // Always include primary

        var lowerQuery = query.ToLowerInvariant();

        if (ContainsFileSystemIntent(lowerQuery))
            candidates.Add("filesystem");

        if (ContainsMicrosoftLearnIntent(lowerQuery))
            candidates.Add("microsoft-learn");

        // Check if combined tools exceed limit
        var toolCount = 0;
        var selectedServers = new List<string>();

        foreach (var server in candidates)
        {
            var serverTools = await _toolCatalog.GetToolsFromServersAsync(new[] { server }, cancellationToken);
            var serverToolCount = serverTools.Count();

            if (toolCount + serverToolCount <= MaxToolsPerRequest)
            {
                selectedServers.Add(server);
                toolCount += serverToolCount;
            }
            else
            {
                _logger.LogWarning(
                    "Skipping {Server}: would exceed tool limit ({Current} + {Additional} > {Max})",
                    server, toolCount, serverToolCount, MaxToolsPerRequest);
                break;
            }
        }

        return selectedServers;
    }

    private static bool ContainsFileSystemIntent(string query)
    {
        var fileSystemKeywords = new[]
        {
            "file", "folder", "directory", "document", "documents folder",
            "temp", "desktop", "download", "path", "read file", "write file",
            "create file", "delete file", "move file", "copy file", "list files",
            "c:\\", "g:\\", ".txt", ".pdf", ".docx", ".xlsx"
        };

        return fileSystemKeywords.Any(keyword => query.Contains(keyword));
    }

    private static bool ContainsMicrosoftLearnIntent(string query)
    {
        var msLearnKeywords = new[]
        {
            "documentation", "docs", "microsoft", ".net", "c#", "csharp",
            "azure", "asp.net", "blazor", "entity framework", "learn",
            "tutorial", "api reference", "guide"
        };

        return msLearnKeywords.Any(keyword => query.Contains(keyword));
    }
}