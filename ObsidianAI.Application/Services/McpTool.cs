/// <summary>
/// Represents a tool discovered from an MCP server, including the originating server name.
/// </summary>
namespace ObsidianAI.Application.Services;

public sealed record McpTool
{
    /// <summary>
    /// Name of the MCP server this tool originates from (e.g., "obsidian", "filesystem", "microsoft-learn").
    /// </summary>
    public required string ServerName { get; init; }

    /// <summary>
    /// The raw tool descriptor object returned by the MCP client. Shape depends on the MCP server.
    /// </summary>
    public required object Tool { get; init; }

    /// <summary>
    /// Convenience accessor that attempts to read a "Name" property from the tool descriptor via reflection.
    /// Returns null if not available.
    /// </summary>
    public string? Name => Tool?.GetType().GetProperty("Name")?.GetValue(Tool) as string;
}