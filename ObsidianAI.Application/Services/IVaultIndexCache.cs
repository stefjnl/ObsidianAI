using System;
using System.Collections.Generic;
using ObsidianAI.Application.Contracts;

namespace ObsidianAI.Application.Services;

/// <summary>
/// Provides caching for vault listings and aggregated path indexes to avoid redundant MCP lookups.
/// </summary>
public interface IVaultIndexCache
{
    /// <summary>
    /// Attempts to retrieve a cached vault listing entry for the provided key.
    /// </summary>
    /// <param name="key">Normalized cache key (e.g., a directory path or <see cref="VaultIndexCacheKeys.Root"/>).</param>
    /// <param name="entry">The cached entry when available and not expired.</param>
    /// <returns><c>true</c> when a non-expired entry exists; otherwise <c>false</c>.</returns>
    bool TryGet(string key, out VaultIndexEntry entry);

    /// <summary>
    /// Stores a vault listing snapshot.
    /// </summary>
    /// <param name="key">Normalized cache key.</param>
    /// <param name="paths">Raw vault paths exactly as returned by the MCP tool.</param>
    /// <param name="items">Parsed vault items derived from the MCP response.</param>
    /// <param name="ttl">The absolute lifetime for the cache entry.</param>
    void Set(string key, IReadOnlyList<string> paths, IReadOnlyList<VaultItemDto> items, TimeSpan ttl);

    /// <summary>
    /// Invalidates a specific cached entry.
    /// </summary>
    /// <param name="key">The cache key to invalidate.</param>
    void Invalidate(string key);

    /// <summary>
    /// Clears the entire cache.
    /// </summary>
    void InvalidateAll();
}

/// <summary>
/// Provides convenient cache keys used by the vault index cache.
/// </summary>
public static class VaultIndexCacheKeys
{
    /// <summary>
    /// Cache key used for the full vault listing returned by <c>obsidian_list_files_in_vault</c>.
    /// </summary>
    public const string Root = "__vault_root__";
}

/// <summary>
/// Represents a cached vault listing snapshot.
/// </summary>
/// <param name="Paths">Raw vault paths (including any emoji or trailing slash semantics).</param>
/// <param name="Items">Parsed vault item DTOs associated with the listing.</param>
/// <param name="ExpiresAt">UTC timestamp when the entry expires.</param>
public sealed record VaultIndexEntry(IReadOnlyList<string> Paths, IReadOnlyList<VaultItemDto> Items, DateTimeOffset ExpiresAt)
{
    /// <summary>
    /// Indicates whether the entry has expired.
    /// </summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
}
