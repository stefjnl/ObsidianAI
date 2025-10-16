using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ObsidianAI.Domain.Services;

namespace ObsidianAI.Application.Services;

/// <summary>
/// Resolves user-supplied file and folder identifiers to canonical vault paths using MCP tooling.
/// </summary>
public sealed class VaultPathResolver : IVaultPathResolver
{
    private readonly IMcpClientProvider _mcpClientProvider;
    private readonly IVaultPathNormalizer _vaultPathNormalizer;
    private readonly ILogger<VaultPathResolver> _logger;

    public VaultPathResolver(
        IMcpClientProvider mcpClientProvider,
        IVaultPathNormalizer vaultPathNormalizer,
        ILogger<VaultPathResolver> logger)
    {
        _mcpClientProvider = mcpClientProvider ?? throw new ArgumentNullException(nameof(mcpClientProvider));
        _vaultPathNormalizer = vaultPathNormalizer ?? throw new ArgumentNullException(nameof(vaultPathNormalizer));
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<VaultPathResolver>.Instance;
    }

    /// <inheritdoc />
    public async Task<string> ResolveAsync(string candidatePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(candidatePath))
        {
            return string.Empty;
        }

        var fallback = _vaultPathNormalizer.Normalize(candidatePath);
        var comparisonKey = _vaultPathNormalizer.CreateMatchKey(candidatePath);
        if (string.IsNullOrEmpty(comparisonKey))
        {
            return fallback;
        }

        try
        {
            var client = await _mcpClientProvider.GetClientAsync(cancellationToken).ConfigureAwait(false);
            if (client is null)
            {
                _logger.LogDebug("MCP client unavailable; returning normalized fallback for '{Candidate}'", candidatePath);
                return fallback;
            }

            var toolResult = await client.CallToolAsync(
                "obsidian_list_files_in_vault",
                new Dictionary<string, object?>(),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var vaultPaths = ExtractVaultPaths(toolResult.Content);
            if (vaultPaths.Count == 0)
            {
                _logger.LogDebug("obsidian_list_files_in_vault returned no paths; using fallback for '{Candidate}'", candidatePath);
                return fallback;
            }

            var match = FindBestMatch(vaultPaths, comparisonKey);
            if (match is not null)
            {
                return match;
            }

            var fallbackKey = _vaultPathNormalizer.CreateMatchKey(fallback);
            if (!string.Equals(fallbackKey, comparisonKey, StringComparison.Ordinal))
            {
                match = FindBestMatch(vaultPaths, fallbackKey);
                if (match is not null)
                {
                    return match;
                }
            }

            _logger.LogDebug("No vault path matched '{Candidate}', returning fallback '{Fallback}'", candidatePath, fallback);
            return fallback;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve vault path for '{Candidate}'. Returning fallback normalized path.", candidatePath);
            return fallback;
        }
    }

    private IReadOnlyList<string> ExtractVaultPaths(IEnumerable<ContentBlock>? content)
    {
        if (content is null)
        {
            return Array.Empty<string>();
        }

        var textBlock = content.OfType<TextContentBlock>().FirstOrDefault();
        if (textBlock is null || string.IsNullOrWhiteSpace(textBlock.Text))
        {
            return Array.Empty<string>();
        }

        var text = textBlock.Text.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<string>();
        }

        if (TryParseJsonArray(text, out var parsed))
        {
            return parsed;
        }

        var splitPaths = text
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(segment => segment.Trim())
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return splitPaths;
    }

    private static bool TryParseJsonArray(string text, out IReadOnlyList<string> paths)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                var list = doc.RootElement
                    .EnumerateArray()
                    .Select(element => element.ValueKind == JsonValueKind.String ? element.GetString() : null)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value!.Trim())
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();

                paths = list;
                return list.Length > 0;
            }
        }
        catch (JsonException)
        {
            // Ignore JSON parsing issues and fall back to newline handling.
        }

        paths = Array.Empty<string>();
        return false;
    }

    private string? FindBestMatch(IEnumerable<string> vaultPaths, string normalizedInput)
    {
        var candidates = vaultPaths
            .Select(path => new
            {
                Original = path,
                Key = _vaultPathNormalizer.CreateMatchKey(path)
            })
            .Where(candidate => !string.IsNullOrEmpty(candidate.Key))
            .ToList();

        var exact = candidates.FirstOrDefault(candidate => string.Equals(candidate.Key, normalizedInput, StringComparison.Ordinal));
        if (exact is not null)
        {
            return NormalizeDirectoryPath(exact.Original);
        }

        var containsMatches = candidates
            .Where(candidate => candidate.Key.Contains(normalizedInput, StringComparison.Ordinal))
            .OrderBy(candidate => candidate.Key.Length)
            .ThenBy(candidate => candidate.Original.Length)
            .ToList();

        if (containsMatches.Count == 1)
        {
            return NormalizeDirectoryPath(containsMatches[0].Original);
        }

        if (containsMatches.Count > 1)
        {
            return NormalizeDirectoryPath(containsMatches.First().Original);
        }

        var containedByInput = candidates
            .Where(candidate => normalizedInput.Contains(candidate.Key, StringComparison.Ordinal))
            .OrderByDescending(candidate => candidate.Key.Length)
            .ThenBy(candidate => candidate.Original.Length)
            .FirstOrDefault();

        return containedByInput is null ? null : NormalizeDirectoryPath(containedByInput.Original);
    }

    private static string NormalizeDirectoryPath(string path)
    {
        return path.EndsWith("/", StringComparison.Ordinal) ? path.TrimEnd('/') : path;
    }
}
