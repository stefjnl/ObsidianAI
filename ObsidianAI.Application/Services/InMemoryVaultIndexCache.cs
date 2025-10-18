using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ObsidianAI.Application.Contracts;

namespace ObsidianAI.Application.Services;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IVaultIndexCache"/> suitable for a single process deployment.
/// </summary>
public sealed class InMemoryVaultIndexCache : IVaultIndexCache
{
    private readonly ConcurrentDictionary<string, VaultIndexEntry> _entries = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public bool TryGet(string key, out VaultIndexEntry entry)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            entry = default!;
            return false;
        }

        if (_entries.TryGetValue(key, out var cached) && !cached.IsExpired)
        {
            entry = cached;
            return true;
        }

        if (cached is not null)
        {
            _entries.TryRemove(key, out _);
        }

        entry = default!;
        return false;
    }

    /// <inheritdoc />
    public void Set(string key, IReadOnlyList<string> paths, IReadOnlyList<VaultItemDto> items, TimeSpan ttl)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        var expiration = DateTimeOffset.UtcNow.Add(ttl <= TimeSpan.Zero ? TimeSpan.FromSeconds(1) : ttl);
        var entry = new VaultIndexEntry(paths, items, expiration);
        _entries[key] = entry;
    }

    /// <inheritdoc />
    public void Invalidate(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        _entries.TryRemove(key, out _);
    }

    /// <inheritdoc />
    public void InvalidateAll() => _entries.Clear();
}
