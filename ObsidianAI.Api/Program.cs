using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ObsidianAI.Api.Models;
using OpenAI;
using System.ClientModel;
using System.Text;

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

var chatClient = lmStudioClient.GetChatClient("openai/gpt-oss-20b");

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

app.MapPost("/chat/stream", async (ChatRequest request, HttpContext context) =>
{
    context.Response.ContentType = "text/plain";
    context.Response.Headers.CacheControl = "no-cache";
    context.Response.Headers.Connection = "keep-alive";

    var responseStream = agent.RunStreamingAsync(request.Message);
    var enumerableResponseStream = (System.Collections.Generic.IAsyncEnumerable<dynamic>)responseStream;
    await foreach (var update in enumerableResponseStream)
    {
        var text = update?.Text?.ToString() ?? "";
        var data = Encoding.UTF8.GetBytes(text);
        await context.Response.Body.WriteAsync(data);
        await context.Response.Body.FlushAsync();
    }

    return Results.Ok();
});

app.MapPost("/vault/search", (SearchRequest request) =>
{
    // Placeholder logic
    var results = new List<SearchResult>
    {
        new SearchResult("docs/sample.md", 0.9f, "This is a sample search result.")
    };
    return Results.Ok(new SearchResponse(results));
});

app.MapPost("/vault/reorganize", (ReorganizeRequest request) =>
{
    // Placeholder logic
    return Results.Ok(new ReorganizeResponse("Completed", 10));
});

app.Run();
