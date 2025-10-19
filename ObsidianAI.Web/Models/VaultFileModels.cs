namespace ObsidianAI.Web.Models;

/// <summary>
/// Represents a file in the Obsidian vault
/// </summary>
public record VaultFile(string Name, string Path, string Content, DateTime LastModified)
{
    /// <summary>
    /// File name including extension
    /// </summary>
    public string Name { get; init; } = Name;

    /// <summary>
    /// Full path relative to vault root
    /// </summary>
    public string Path { get; init; } = Path;

    /// <summary>
    /// File content (Markdown text)
    /// </summary>
    public string Content { get; init; } = Content;

    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime LastModified { get; init; } = LastModified;

    /// <summary>
    /// Optional size in bytes
    /// </summary>
    public long? Size { get; init; }
}

/// <summary>
/// Search result container
/// </summary>
public record SearchResult(string Query, IReadOnlyList<VaultFile> Results)
{
    /// <summary>
    /// Original search query
    /// </summary>
    public string Query { get; init; } = Query;

    /// <summary>
    /// Matching files
    /// </summary>
    public IReadOnlyList<VaultFile> Results { get; init; } = Results;

    /// <summary>
    /// Total number of results
    /// </summary>
    public int TotalCount => Results.Count;
}

/// <summary>
/// File content response from /vault/read endpoint
/// </summary>
public record VaultFileContent(string Path, string Content)
{
    /// <summary>
    /// File path
    /// </summary>
    public string Path { get; init; } = Path;

    /// <summary>
    /// File content
    /// </summary>
    public string Content { get; init; } = Content;
}
