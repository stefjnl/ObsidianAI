namespace ObsidianAI.Application.Contracts;

/// <summary>
/// Represents the response from a vault search operation.
/// </summary>
/// <param name="Results">List of search result items.</param>
/// <param name="TotalCount">Total number of results found.</param>
/// <param name="SearchTimeMs">Time taken to perform the search in milliseconds.</param>
public sealed record SearchResponse(System.Collections.Generic.List<SearchResultItem> Results, int TotalCount, long SearchTimeMs);