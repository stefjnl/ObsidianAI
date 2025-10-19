namespace ObsidianAI.Infrastructure.Configuration;

public sealed class McpServersConfiguration
{
    public const string SectionName = "McpServers";
    
    public Dictionary<string, McpServerOptions> Servers { get; set; } = new();
}

public sealed class McpServerOptions
{
    public required string Endpoint { get; set; }
    public bool Enabled { get; set; } = true;
    public Dictionary<string, string> Metadata { get; set; } = new();
}
