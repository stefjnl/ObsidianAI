using ModelContextProtocol.Client;
using ObsidianAI.Api.Models;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ObsidianAI.Api.Services;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults (health checks, telemetry)
builder.AddServiceDefaults();

// Configure strongly-typed AppSettings
builder.Services.Configure<AppSettings>(builder.Configuration);

// Register ILlmClientFactory singleton based on LLM:Provider configuration
builder.Services.AddSingleton<ILlmClientFactory>(sp =>
{
    var appSettings = sp.GetRequiredService<IOptions<AppSettings>>();
    var provider = appSettings.Value.LLM.Provider?.Trim() ?? "LMStudio";
    return provider.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase)
        ? new OpenRouterClientFactory(appSettings)
        : new LmStudioClientFactory(appSettings);
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
    var logger = sp.GetRequiredService<ILogger<ObsidianAssistantService>>();
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

    return new ObsidianAssistantService(llmFactory, mcpClient, instructions, logger);
});

var app = builder.Build();
// Startup log for configured LLM provider and model
var llmFactory = app.Services.GetRequiredService<ILlmClientFactory>();
var appSettings = app.Services.GetRequiredService<IOptions<AppSettings>>();
var providerName = appSettings.Value.LLM.Provider?.Trim() ?? "LMStudio";
var modelName = llmFactory.GetModelName();
app.Logger.LogInformation("Using LLM provider: {Provider}, Model: {Model}", providerName, modelName);
app.MapDefaultEndpoints();

// Expose current LLM provider to frontend
app.MapGet("/api/llm/provider", (IOptions<AppSettings> appSettings) =>
{
    var provider = appSettings.Value.LLM.Provider?.Trim() ?? "LMStudio";
    return Results.Ok(new { provider });
});

// 5. Create chat endpoint using ObsidianAssistantService
app.MapPost("/chat", async (ChatRequest request, ObsidianAssistantService assistant) =>
{
    var responseText = await assistant.ChatAsync(request);
    var fileOperationResult = ExtractFileOperationResult(responseText);

    var response = new
    {
        text = responseText,
        fileOperationResult = fileOperationResult
    };

    return Results.Ok(response);
});

// Helper method to extract file operation results from response text using regex
static FileOperationData? ExtractFileOperationResult(string response)
{
    if (string.IsNullOrEmpty(response))
        return null;

    // Regex patterns to detect file operations
    var patterns = new[]
    {
        // Pattern for file creation: "created file 'path'" or "created the file 'path'"
        new { Regex = @"(?:created|made|established)\s+(?:the\s+)?(?:file|note)\s+['""]?([^'""\n]+)['""]?", Action = "Created" },
        // Pattern for file modification: "modified file 'path'" or "updated the file 'path'"
        new { Regex = @"(?:modified|updated|edited|changed)\s+(?:the\s+)?(?:file|note)\s+['""]?([^'""\n]+)['""]?", Action = "Modified" },
        // Pattern for file appending: "appended to file 'path'" or "added to the file 'path'"
        new { Regex = @"(?:appended|added)\s+(?:to\s+)?(?:the\s+)?(?:file|note)\s+['""]?([^'""\n]+)['""]?", Action = "Appended" },
        // Pattern for file deletion: "deleted file 'path'" or "removed the file 'path'"
        new { Regex = @"(?:deleted|removed|erased)\s+(?:the\s+)?(?:file|note)\s+['""]?([^'""\n]+)['""]?", Action = "Deleted" },
        // Pattern for file moving: "moved file 'path' to 'path'" or "relocated the file 'path'"
        new { Regex = @"(?:moved|relocated|transferred)\s+(?:the\s+)?(?:file|note)\s+['""]?([^'""\n]+)['""]?", Action = "Moved" }
    };

    foreach (var pattern in patterns)
    {
        var match = System.Text.RegularExpressions.Regex.Match(response, pattern.Regex, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success && match.Groups.Count > 1)
        {
            var filePath = match.Groups[1].Value.Trim();
            if (!string.IsNullOrEmpty(filePath))
            {
                return new FileOperationData(pattern.Action, filePath);
            }
        }
    }

    // If no pattern matched, return null
    return null;
}

app.MapPost("/chat/stream", async (ChatRequest request, HttpContext context, ObsidianAssistantService assistant, ILogger<Program> logger) =>
{
    context.Response.ContentType = "text/event-stream";
    context.Response.Headers.CacheControl = "no-cache";
    context.Response.Headers.Connection = "keep-alive";

    logger.LogInformation("Starting SSE stream for message: {Message}", request.Message);
    var updateCount = 0;

    try
    {
        await foreach (var update in assistant.StreamChatAsync(request, context.RequestAborted))
        {
            updateCount++;

            // Check if this is a tool call message (role = "tool_call")
            if (update.Role == "tool_call")
            {
                logger.LogInformation("Sending tool_call event: {ToolName}", update.Content);
                await context.Response.WriteAsync($"event: tool_call\ndata: {update.Content}\n\n", context.RequestAborted);
                await context.Response.Body.FlushAsync(context.RequestAborted);
            }
            // Send incremental text as SSE data line - ALWAYS with data: prefix
            else if (!string.IsNullOrEmpty(update.Content))
            {
                // Log with escaped characters
                var escapedContent = update.Content.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
                logger.LogInformation("Sending token #{Count}: RAW='{Escaped}' (Length={Length})",
                    updateCount,
                    escapedContent.Length > 100 ? escapedContent.Substring(0, 100) + "..." : escapedContent,
                    update.Content.Length);

                // CRITICAL: Always send with data: prefix immediately
                await context.Response.WriteAsync($"data: {update.Content}\n\n", context.RequestAborted);
                await context.Response.Body.FlushAsync(context.RequestAborted);
            }
        }

        // Send completion marker
        logger.LogInformation("Stream complete. Sending [DONE] marker after {Count} updates", updateCount);
        await context.Response.WriteAsync("data: [DONE]\n\n", context.RequestAborted);
        await context.Response.Body.FlushAsync(context.RequestAborted);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during streaming after {Count} updates", updateCount);

        // Send error event
        await context.Response.WriteAsync($"event: error\ndata: {ex.Message}\n\n", context.RequestAborted);
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

app.MapPost("/vault/modify", async (ModifyRequest request, McpClient mcpClient, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Executing modify operation: {Operation} on {FilePath}", request.Operation, request.FilePath);

        // Normalize operation name
        var op = request.Operation.ToLowerInvariant() switch
        {
            "append" => "obsidian_append_content",
            "modify" or "patch" or "write" => "obsidian_patch_content",
            "delete" => "obsidian_delete_file",
            "create" => "obsidian_create_file",
            _ => throw new ArgumentException($"Unsupported operation: {request.Operation}")
        };

        // Prepare tool arguments as IReadOnlyDictionary<string, object?>
        IReadOnlyDictionary<string, object?> arguments = op switch
        {
            "obsidian_append_content" => new Dictionary<string, object?>
            {
                ["filepath"] = request.FilePath,
                ["content"] = request.Content
            },
            "obsidian_patch_content" => new Dictionary<string, object?>
            {
                ["filepath"] = request.FilePath,
                ["content"] = request.Content,
                ["operation"] = "append" // Default patch operation; can be refined later
            },
            "obsidian_delete_file" => new Dictionary<string, object?>
            {
                ["filepath"] = request.FilePath
            },
            "obsidian_create_file" => new Dictionary<string, object?>
            {
                ["filepath"] = request.FilePath,
                ["content"] = request.Content
            },
            _ => throw new InvalidOperationException($"Unexpected tool name: {op}")
        };

        // Call the MCP tool
        var result = await mcpClient.CallToolAsync(op, arguments);

        string responseMessage = "Operation failed: No response from tool.";
        // Correctly determine success based on the IsError flag
        bool isSuccess = !(result.IsError ?? false);

        if (result.Content?.FirstOrDefault() is ModelContextProtocol.Protocol.TextContentBlock textBlock)
        {
            responseMessage = textBlock.Text;
        }

        if (!isSuccess)
        {
            logger.LogError("MCP tool {ToolName} returned error: {Error}", op, responseMessage);
            return Results.Ok(new ModifyResponse(false, responseMessage, request.FilePath));
        }

        logger.LogInformation("MCP tool {ToolName} completed successfully: {Message}", op, responseMessage);
        return Results.Ok(new ModifyResponse(true, responseMessage, request.FilePath));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error executing modify operation: {Operation} on {FilePath}", request.Operation, request.FilePath);
        return Results.Ok(new ModifyResponse(false, ex.Message, request.FilePath));
    }
});

app.Run();
