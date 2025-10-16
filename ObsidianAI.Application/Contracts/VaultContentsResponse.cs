namespace ObsidianAI.Application.Contracts;

/// <summary>
/// Represents the response from browsing vault contents.
/// </summary>
/// <param name="Items">List of vault items (files and folders).</param>
/// <param name="CurrentPath">The current path being browsed.</param>
public sealed record VaultContentsResponse(
    System.Collections.Generic.List<VaultItemDto> Items,
    string CurrentPath);

/// <summary>
/// Represents a single item (file or folder) in the vault.
/// </summary>
/// <param name="Name">The display name of the item.</param>
/// <param name="Path">The full vault path of the item.</param>
/// <param name="Type">The type of item (File or Folder).</param>
/// <param name="Extension">The file extension (for files only).</param>
/// <param name="Size">The file size in bytes (for files only).</param>
/// <param name="LastModified">The last modified timestamp.</param>
public sealed record VaultItemDto(
    string Name,
    string Path,
    VaultItemType Type,
    string? Extension,
    long? Size,
    System.DateTime? LastModified);

/// <summary>
/// Enumeration of vault item types.
/// </summary>
public enum VaultItemType
{
    /// <summary>
    /// Represents a file in the vault.
    /// </summary>
    File,

    /// <summary>
    /// Represents a folder in the vault.
    /// </summary>
    Folder
}
