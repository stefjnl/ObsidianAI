namespace ObsidianAI.Web.Models;

/// <summary>
/// Represents vault contents data for the web UI.
/// </summary>
public record VaultContentsData
{
    public List<VaultItemData> Items { get; init; } = new();
    public string CurrentPath { get; init; } = string.Empty;
}

/// <summary>
/// Represents a single vault item (file or folder) for the web UI.
/// </summary>
public record VaultItemData
{
    public string Name { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public VaultItemType Type { get; init; }
    public string? Extension { get; init; }
    public long? Size { get; init; }
    public DateTime? LastModified { get; init; }
    public string Icon { get; set; } = "📄";
    public bool IsExpanded { get; set; }
    public List<VaultItemData> Children { get; set; } = new();
}

/// <summary>
/// Enumeration of vault item types.
/// </summary>
public enum VaultItemType
{
    File,
    Folder
}

/// <summary>
/// Response model for vault browsing operations (alias for VaultContentsData)
/// </summary>
public record VaultBrowserResponse : VaultContentsData;
