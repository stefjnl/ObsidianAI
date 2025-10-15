using ModelContextProtocol.Client;
using ObsidianAI.Api.Models;
using ObsidianAI.Api.Services;
using ObsidianAI.Api.Streaming;
using ObsidianAI.Infrastructure.Configuration;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using ObsidianAI.Infrastructure.DI;
using ObsidianAI.Application.DI;
using ObsidianAI.Application.UseCases;
using ObsidianAI.Domain.Models;
using ObsidianAI.Domain.Ports;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults (health checks, telemetry)
builder.AddServiceDefaults();

// Configure strongly-typed AppSettings
builder.Services.Configure<AppSettings>(builder.Configuration);

// Register MCP client service
builder.Services.AddSingleton<ObsidianAI.Api.Services.McpClientService>();
builder.Services.AddHostedService<ObsidianAI.Api.Services.McpClientService>(); // Register as hosted service for proper startup initialization
builder.Services.AddSingleton(sp => sp.GetRequiredService<McpClientService>().Client!);

// Register infrastructure and application services
builder.Services.AddObsidianAI(builder.Configuration);
builder.Services.AddObsidianApplication();

// Register LLM client factory
builder.Services.AddSingleton<ILlmClientFactory>(sp =>
{
    var appSettings = sp.GetRequiredService<IOptions<AppSettings>>();
    var provider = appSettings.Value.LLM.Provider?.Trim() ?? "LMStudio";
    return provider.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase)
        ? new OpenRouterClientFactory(appSettings)
        : new LmStudioClientFactory(appSettings);
});

var app = builder.Build();
app.MapDefaultEndpoints();

// Startup log for configured LLM provider and model
var llmFactory = app.Services.GetRequiredService<ILlmClientFactory>();
var appSettings = app.Services.GetRequiredService<IOptions<AppSettings>>();
var providerName = appSettings.Value.LLM.Provider?.Trim() ?? "LMStudio";
var modelName = llmFactory.GetModelName();
app.Logger.LogInformation("Using LLM provider: {Provider}, Model: {Model}", providerName, modelName);

// Expose current LLM provider to frontend
app.MapGet("/api/llm/provider", (IOptions<AppSettings> appSettings) =>
{
    var provider = appSettings.Value.LLM.Provider?.Trim() ?? "LMStudio";
    return Results.Ok(new { provider });
});

// Create chat endpoint using use cases
app.MapPost("/chat", async (ChatRequest request, StartChatUseCase useCase) =>
{
    var instructions = @"You are a helpful assistant that manages an Obsidian vault. Follow these rules exactly:

FILE RESOLUTION:
When user mentions a filename:
1. Call obsidian_list_files_in_vault() to get all paths
2. Normalize: remove emojis, lowercase, trim, add .md if missing
3. Match normalized user input to normalized vault filenames
4. Use EXACT vault path (with emoji) in tool calls
5. If multiple matches: list options and ask which one
6. If no match: inform user file doesn't exist

WHEN USER SAYS ""what's in my vault"" OR ""list files"":
- Call obsidian_list_files_in_vault() immediately
- Display the results as a formatted list
- Add a helpful closing like ""Let me know if you'd like to read any of these files!""
- DO NOT ask ""which file would you like to read"" - they didn't ask to read anything yet

WHEN USER SAYS ""read [filename]"" OR ""show me [filename]"" OR ""what's in [filename]"":
- Find the file using the resolution strategy above
- Call the appropriate read tool immediately
- Display the contents
- DO NOT ask for confirmation

WHEN USER SAYS ""append to [filename]"" OR ""create [filename]"" OR ""delete [filename]"":
- Find the file using resolution strategy
- Respond: ""I will [action] to/from the file: [exact emoji path]. Please confirm to proceed.""
- Wait for user confirmation

EXAMPLES:
❌ BAD: ""I have listed the files. Which file would you like to read?""
✅ GOOD: ""Here are the files in your vault: [list]. Let me know if you'd like to explore any of them!""

❌ BAD: ""I found Project Ideas.md. Should I read it?""
✅ GOOD: ""Here are the contents of 💡 Project Ideas.md: [contents]""

CRITICAL:
- Use EXACT paths with emojis in tool calls
- Never use search tools to find filenames
- List operations don't need confirmation
- Read operations don't need confirmation
- Write/modify/delete operations need confirmation";

    // Map history to Domain ConversationMessage list
    var history = request.History?.Select(h => new ConversationMessage(
        h.Role.Equals("user", StringComparison.OrdinalIgnoreCase) ? ParticipantRole.User : ParticipantRole.Assistant,
        h.Content)).ToList();

    var input = new ChatInput(request.Message, history);
    var result = await useCase.ExecuteAsync(input, instructions);

    return Results.Ok(new
    {
        text = result.Text,
        fileOperationResult = result.FileOperation == null ? null : new FileOperationData(result.FileOperation.Action, result.FileOperation.FilePath)
    });
});


app.MapPost("/chat/stream", async (ChatRequest request, HttpContext context, StreamChatUseCase useCase, ILogger<Program> logger) =>
{
    logger.LogInformation("Starting SSE stream for message: {Message}", request.Message);

    var instructions = @"You are a helpful assistant that manages an Obsidian vault. Follow these rules exactly:

FILE RESOLUTION:
When user mentions a filename:
1. Call obsidian_list_files_in_vault() to get all paths
2. Normalize: remove emojis, lowercase, trim, add .md if missing
3. Match normalized user input to normalized vault filenames
4. Use EXACT vault path (with emoji) in tool calls
5. If multiple matches: list options and ask which one
6. If no match: inform user file doesn't exist

WHEN USER SAYS ""what's in my vault"" OR ""list files"":
- Call obsidian_list_files_in_vault() immediately
- Display the results as a formatted list
- Add a helpful closing like ""Let me know if you'd like to read any of these files!""
- DO NOT ask ""which file would you like to read"" - they didn't ask to read anything yet

WHEN USER SAYS ""read [filename]"" OR ""show me [filename]"" OR ""what's in [filename]"":
- Find the file using the resolution strategy above
- Call the appropriate read tool immediately
- Display the contents
- DO NOT ask for confirmation

WHEN USER SAYS ""append to [filename]"" OR ""create [filename]"" OR ""delete [filename]"":
- Find the file using resolution strategy
- Respond: ""I will [action] to/from the file: [exact emoji path]. Please confirm to proceed.""
- Wait for user confirmation

EXAMPLES:
❌ BAD: ""I have listed the files. Which file would you like to read?""
✅ GOOD: ""Here are the files in your vault: [list]. Let me know if you'd like to explore any of them!""

❌ BAD: ""I found Project Ideas.md. Should I read it?""
✅ GOOD: ""Here are the contents of 💡 Project Ideas.md: [contents]""

CRITICAL:
- Use EXACT paths with emojis in tool calls
- Never use search tools to find filenames
- List operations don't need confirmation
- Read operations don't need confirmation
- Write/modify/delete operations need confirmation";

    // Map history to Domain ConversationMessage list
    var history = request.History?.Select(h => new ConversationMessage(
        h.Role.Equals("user", StringComparison.OrdinalIgnoreCase) ? ParticipantRole.User : ParticipantRole.Assistant,
        h.Content)).ToList();

    var input = new ChatInput(request.Message, history);
    var stream = useCase.ExecuteAsync(input, instructions, context.RequestAborted);

    await StreamingEventWriter.WriteAsync(context, stream, logger, context.RequestAborted);
});

app.MapPost("/vault/search", async (SearchRequest request, SearchVaultUseCase useCase) =>
{
    var result = await useCase.ExecuteAsync(request.Query);
    // Map to API SearchResponse shape
    var apiResults = result.Results.Select(r => new SearchResult(r.Path, (float)r.Score, r.Preview)).ToList();
    return Results.Ok(new SearchResponse(apiResults));
});

app.MapPost("/vault/reorganize", (ReorganizeRequest request) =>
{
    // Placeholder logic
    return Results.Ok(new ReorganizeResponse("Completed", 10));
});

app.MapPost("/vault/modify", async (ModifyRequest request, ModifyVaultUseCase useCase) =>
{
    var result = await useCase.ExecuteAsync(request.Operation, request.FilePath, request.Content);
    return Results.Ok(new ModifyResponse(result.Success, result.Message, result.FilePath));
});

app.Run();
