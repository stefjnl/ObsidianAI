using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ObsidianAI.Application.Services;

/// <summary>
/// Provides cached access to merged MCP tool descriptors across the Obsidian vault and Microsoft Learn endpoints.
/// </summary>
public interface IMcpToolCatalog
{
    /// <summary>
    /// Retrieves the current merged tool snapshot, warming the cache when necessary.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A snapshot describing the merged tool collection.</returns>
    Task<McpToolCatalogSnapshot> GetToolsAsync(CancellationToken ct = default);

    /// <summary>
    /// Forces cache invalidation so the next call will refresh tool metadata.
    /// </summary>
    void Invalidate();
}

/// <summary>
/// Describes the current merged tool catalog state.
/// </summary>
/// <param name="Tools">Merged tools exposed through the MCP endpoints.</param>
/// <param name="ObsidianToolCount">Number of Obsidian vault tools present in the snapshot.</param>
/// <param name="MicrosoftLearnToolCount">Number of Microsoft Learn MCP tools present in the snapshot.</param>
/// <param name="ExpiresAt">UTC timestamp when the snapshot becomes stale.</param>
public sealed record McpToolCatalogSnapshot(IReadOnlyList<object> Tools, int ObsidianToolCount, int MicrosoftLearnToolCount, DateTimeOffset ExpiresAt);
