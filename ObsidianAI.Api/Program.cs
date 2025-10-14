using ModelContextProtocol.Client;
using ObsidianAI.Api.Models;
using System.Text;
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

// Register assistant service as singleton
builder.Services.AddSingleton<ObsidianAssistantService>();

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
    context.Response.ContentType = "text/plain";
    context.Response.Headers.CacheControl = "no-cache";
    context.Response.Headers.Connection = "keep-alive";

    await foreach (var chunk in assistant.StreamChatAsync(request, context.RequestAborted))
    {
        var data = Encoding.UTF8.GetBytes(chunk);
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
