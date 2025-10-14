using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults (health checks, telemetry)
builder.AddServiceDefaults();

// 1. Connect to LM Studio (OpenAI-compatible)
var lmStudioClient = new OpenAIClient(
    new ApiKeyCredential("lm-studio"),
    new OpenAIClientOptions
    {
        Endpoint = new Uri("http://localhost:1234/v1")
    }
);

var chatClient = lmStudioClient.GetChatClient("your-model-name");

// 2. Connect to MCP server via Docker Gateway with Streaming Transport
// First, ensure Docker MCP Gateway is running with streaming:
// docker mcp gateway run --transport streaming --port 8033

var clientTransport = new HttpClientTransport(
    new HttpClientTransportOptions
    {
        Endpoint = new Uri("http://localhost:8033/mcp")
    }
);

await using var mcpClient = await McpClient.CreateAsync(clientTransport);

// 3. Get Obsidian tools from MCP server
var obsidianTools = await mcpClient.ListToolsAsync();

Console.WriteLine($"Connected to MCP server. Found {obsidianTools.Count} tools.");

// 4. Create Agent with both LLM and tools
var agent = chatClient.CreateAIAgent(
    name: "ObsidianAssistant",
    instructions: "You help users query and organize their Obsidian vault. Use the available tools to search, read, and modify notes.",
    tools: [.. obsidianTools.Cast<AITool>()]
);

var app = builder.Build();
app.MapDefaultEndpoints();

// 5. Create chat endpoint
app.MapPost("/chat", async (ChatRequest request) =>
{
    var response = await agent.RunAsync(request.Message);
    return Results.Ok(new { text = response.Text });
});

app.Run();

record ChatRequest(string Message);