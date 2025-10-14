namespace ObsidianAI.Web.Models
{
    /// <summary>
    /// Represents a search result from the vault
    /// </summary>
    public record SearchResultData
    {
        /// <summary>
        /// Unique identifier for the search result
        /// </summary>
        public string Id { get; init; } = System.Guid.NewGuid().ToString();
        
        /// <summary>
        /// Title of the found note/file
        /// </summary>
        public string Title { get; init; } = string.Empty;
        
        /// <summary>
        /// Path to the file in the vault
        /// </summary>
        public string FilePath { get; init; } = string.Empty;
        
        /// <summary>
        /// Preview of the content containing the search match
        /// </summary>
        public string Preview { get; init; } = string.Empty;
        
        /// <summary>
        /// Icon to display for the file type
        /// </summary>
        public string Icon { get; init; } = "📝";
        
        /// <summary>
        /// File extension
        /// </summary>
        public string FileExtension { get; init; } = string.Empty;
        
        /// <summary>
        /// Size of the file in bytes
        /// </summary>
        public long FileSize { get; init; }
        
        /// <summary>
        /// Last modified date of the file
        /// </summary>
        public System.DateTime LastModified { get; init; } = System.DateTime.UtcNow;
        
        /// <summary>
        /// List of tags associated with the file
        /// </summary>
        public List<string> Tags { get; init; } = new();
        
        /// <summary>
        /// Relevance score for the search result
        /// </summary>
        public double RelevanceScore { get; init; }
        
        /// <summary>
        /// Whether this result is part of a collection
        /// </summary>
        public bool IsInCollection { get; init; }
    }

    /// <summary>
    /// Represents a collection of search results
    /// </summary>
    public record SearchResultCollection
    {
        /// <summary>
        /// Unique identifier for the collection
        /// </summary>
        public string Id { get; init; } = System.Guid.NewGuid().ToString();
        
        /// <summary>
        /// Name of the collection
        /// </summary>
        public string Name { get; init; } = string.Empty;
        
        /// <summary>
        /// List of search results in this collection
        /// </summary>
        public List<SearchResultData> Results { get; init; } = new();
        
        /// <summary>
        /// Total number of results found
        /// </summary>
        public int TotalCount { get; init; }
        
        /// <summary>
        /// Whether there are more results than shown
        /// </summary>
        public bool HasMore { get; init; }
        
        /// <summary>
        /// Search query that produced these results
        /// </summary>
        public string Query { get; init; } = string.Empty;
        
        /// <summary>
        /// Time taken for the search in milliseconds
        /// </summary>
        public long SearchTimeMs { get; init; }
    }
}