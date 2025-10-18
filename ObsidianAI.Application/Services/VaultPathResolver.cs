using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ObsidianAI.Application.Contracts;
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
    private readonly IVaultIndexCache _vaultIndexCache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(15);

    public VaultPathResolver(
        IMcpClientProvider mcpClientProvider,
        IVaultPathNormalizer vaultPathNormalizer,
        ILogger<VaultPathResolver> logger,
        IVaultIndexCache vaultIndexCache)
    {
        _mcpClientProvider = mcpClientProvider ?? throw new ArgumentNullException(nameof(mcpClientProvider));
        _vaultPathNormalizer = vaultPathNormalizer ?? throw new ArgumentNullException(nameof(vaultPathNormalizer));
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<VaultPathResolver>.Instance;
        _vaultIndexCache = vaultIndexCache ?? throw new ArgumentNullException(nameof(vaultIndexCache));
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

        var fallbackKey = _vaultPathNormalizer.CreateMatchKey(fallback);

        if (_vaultIndexCache.TryGet(VaultIndexCacheKeys.Root, out var cachedEntry))
        {
            var cachedMatch = TryResolveFromPaths(cachedEntry.Paths, comparisonKey, fallbackKey);
            if (cachedMatch is not null)
            {
                _logger.LogDebug("Resolved '{Candidate}' from cached vault index.", candidatePath);
                return cachedMatch;
            }
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

            var contentBlocks = toolResult.Content?.ToList();
            var vaultPaths = VaultToolResponseParser.ExtractPaths(contentBlocks);
            if (vaultPaths.Count == 0)
            {
                _logger.LogDebug("obsidian_list_files_in_vault returned no paths; using fallback for '{Candidate}'", candidatePath);
                return fallback;
            }

            _vaultIndexCache.Set(VaultIndexCacheKeys.Root, vaultPaths, Array.Empty<VaultItemDto>(), CacheDuration);

            var match = TryResolveFromPaths(vaultPaths, comparisonKey, fallbackKey);
            if (match is not null)
            {
                return match;
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

    private string? TryResolveFromPaths(IEnumerable<string> vaultPaths, string comparisonKey, string fallbackKey)
    {
        var match = FindBestMatch(vaultPaths, comparisonKey);
        if (match is not null)
        {
            return match;
        }

        if (!string.Equals(fallbackKey, comparisonKey, StringComparison.Ordinal))
        {
            match = FindBestMatch(vaultPaths, fallbackKey);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static string NormalizeDirectoryPath(string path)
    {
        return path.EndsWith("/", StringComparison.Ordinal) ? path.TrimEnd('/') : path;
    }
}
