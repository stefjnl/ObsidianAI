var builder = DistributedApplication.CreateBuilder(args);

// Add MCP Gateway as an executable resource with auth token
var mcpGateway = builder.AddExecutable("mcp-gateway", "docker", workingDirectory: ".", args: [
    "mcp", "gateway", "run",
    "--transport", "streaming",
    "--port", "8033",
    "--servers", "obsidian"
])
.WithEnvironment("MCP_GATEWAY_AUTH_TOKEN", "0b03f4ceabf6ef04d128e73c827d85b15a872f4603f7b5eadb88f68a25d0f392");

// Add consolidated Web project to orchestration with MCP endpoint configuration
// The MCP gateway runs on localhost:8033 and exposes the MCP endpoint at /mcp
// Web project now hosts both the Blazor UI and REST API endpoints
var web = builder.AddProject<Projects.ObsidianAI_Web>("web")
    .WithEnvironment("MCP_ENDPOINT", "http://localhost:8033/mcp")
    .WithEnvironment("MCP_GATEWAY_AUTH_TOKEN", "0b03f4ceabf6ef04d128e73c827d85b15a872f4603f7b5eadb88f68a25d0f392")
    .WaitFor(mcpGateway);

builder.Build().Run();
