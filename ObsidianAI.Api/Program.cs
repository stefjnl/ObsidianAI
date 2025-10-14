using ModelContextProtocol.Client;
using ObsidianAI.Api.Models;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using ObsidianAI.Api.Services;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults (health checks, telemetry)
builder.AddServiceDefaults();

// Register ILlmClientFactory singleton based on LLM:Provider configuration
builder.Services.AddSingleton<ILlmClientFactory>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var provider = configuration["LLM:Provider"]?.Trim() ?? "LMStudio";
    return provider.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase)
        ? new OpenRouterClientFactory(configuration)
        : new LmStudioClientFactory(configuration);
});

// Register MCP client as singleton using async factory pattern
// We'll create a wrapper service that handles async initialization
builder.Services.AddSingleton<McpClientService>();
builder.Services.AddSingleton<McpClient>(sp => sp.GetRequiredService<McpClientService>().Client);
builder.Services.AddHostedService<McpClientService>(); // Register as hosted service for proper startup initialization

// Register assistant service as singleton with refined agent instructions
builder.Services.AddSingleton<ObsidianAssistantService>(sp =>
{
    var llmFactory = sp.GetRequiredService<ILlmClientFactory>();
    var mcpClient = sp.GetRequiredService<McpClient>();
    var instructions = @"You help users manage their Obsidian vault.

RULES:
- Simple file creation (empty or with specified content): Execute directly, confirm after
- File modification (append/update existing): Ask for confirmation FIRST
- Bulk operations (move multiple files): ALWAYS show preview with confirmation
- Destructive operations (delete): ALWAYS require explicit confirmation

Be efficient. Don't ask unnecessary questions.";
    return new ObsidianAssistantService(llmFactory, mcpClient, instructions);
});

var app = builder.Build();
// Startup log for configured LLM provider and model
var llmFactory = app.Services.GetRequiredService<ILlmClientFactory>();
var providerName = app.Configuration["LLM:Provider"]?.Trim() ?? "LMStudio";
var modelName = llmFactory.GetModelName();
app.Logger.LogInformation("Using LLM provider: {Provider}, Model: {Model}", providerName, modelName);
app.MapDefaultEndpoints();

// Expose current LLM provider to frontend
app.MapGet("/api/llm/provider", (IConfiguration config) =>
{
    var provider = config["LLM:Provider"]?.Trim() ?? "LMStudio";
    return Results.Ok(new { provider });
});

// 5. Create chat endpoint using ObsidianAssistantService
app.MapPost("/chat", async (ChatRequest request, ObsidianAssistantService assistant) =>
{
    var text = await assistant.ChatAsync(request);
    return Results.Ok(new { text });
});

app.MapPost("/chat/stream", async (ChatRequest request, HttpContext context, ObsidianAssistantService assistant) =>
{
    context.Response.ContentType = "application/x-ndjson";
    context.Response.Headers.CacheControl = "no-cache";
    context.Response.Headers.Connection = "keep-alive";

    var options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    await foreach (var message in assistant.StreamChatAsync(request, context.RequestAborted))
    {
        var jsonMessage = JsonSerializer.Serialize(message, options);
        await context.Response.WriteAsync(jsonMessage + "\n", context.RequestAborted);
        await context.Response.Body.FlushAsync(context.RequestAborted);
    }
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
