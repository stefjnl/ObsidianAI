var builder = DistributedApplication.CreateBuilder(args);

// Single MCP Gateway managing multiple servers (Obsidian + Filesystem)
// The gateway handles stdio<->HTTP translation for all MCP servers
// Configure servers in ~/.docker/mcp/config.yaml and registry.yaml
var mcpGateway = builder.AddExecutable("mcp-gateway", "docker", workingDirectory: ".", args: [
    "mcp", "gateway", "run",
    "--transport", "streaming",
    "--port", "8033",
    "--servers", "obsidian,filesystem"  // Multiple servers, single gateway
]);

// Add consolidated Web project to orchestration with MCP endpoint configuration
// The MCP gateway runs on localhost:8033 and exposes the MCP endpoint at /mcp
// Gateway routes requests to appropriate server (obsidian/filesystem) based on tool names
// Web project now hosts both the Blazor UI and REST API endpoints
var web = builder.AddProject<Projects.ObsidianAI_Web>("web")
    .WithEnvironment("MCP_ENDPOINT", "http://localhost:8033/mcp")
    .WaitFor(mcpGateway);

builder.Build().Run();
