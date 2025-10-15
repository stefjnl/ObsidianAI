namespace ObsidianAI.Application.Contracts;

/// <summary>
/// Represents a single search result item from vault search.
/// </summary>
/// <param name="Title">The title or filename of the result.</param>
/// <param name="Path">The vault path to the file.</param>
/// <param name="Preview">A short text preview of the content.</param>
/// <param name="Size">The file size in bytes.</param>
/// <param name="LastModified">The last modified timestamp.</param>
/// <param name="Tags">List of tags associated with the file.</param>
/// <param name="Score">Relevance score for the search result.</param>
public sealed record SearchResultItem(string Title, string Path, string Preview, long Size, System.DateTime LastModified, System.Collections.Generic.List<string> Tags, double Score);