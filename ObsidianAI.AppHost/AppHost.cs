var builder = DistributedApplication.CreateBuilder(args);

// Add MCP Gateway as an executable resource
var mcpGateway = builder.AddExecutable("mcp-gateway", "docker", workingDirectory: ".", args: [
    "mcp", "gateway", "run",
    "--transport", "streaming",
    "--port", "8033",
    "--servers", "obsidian"
]);

// Add API project to orchestration with MCP endpoint configuration
// The MCP gateway runs on localhost:8033 and exposes the MCP endpoint at /mcp
var api = builder.AddProject<Projects.ObsidianAI_Api>("api")
    .WithEnvironment("MCP_ENDPOINT", "http://localhost:8033/mcp")
    .WaitFor(mcpGateway);

// Add Web project to orchestration with reference to API
var web = builder.AddProject<Projects.ObsidianAI_Web>("web")
    .WithReference(api);

builder.Build().Run();
