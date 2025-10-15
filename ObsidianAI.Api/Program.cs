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
    var instructions = @"You are an Obsidian vault assistant with access to file management tools.

# TOOL SELECTION RULES
For queries involving ""list"", ""what"", ""show"", ""find"", or ""search"", you MUST use tools like obsidian_list_files_in_vault or obsidian_simple_search.
Only use tools that create or modify files (e.g., obsidian_append_content, obsidian_patch_content, file creation) when the user explicitly asks to ""create"", ""append"", ""modify"", ""edit"", or ""write"".
You must not use a file writing tool if a user is asking a question or asking to list something.

# TOOL SELECTION PRIORITY
1. **Read operations** (list, search, read): Execute immediately
  - obsidian_list_files_in_vault → when user asks ""list"", ""show folders"", ""what files""
  - obsidian_search_vault → when user asks ""find"", ""search for""
  - obsidian_read_file → when user asks ""show content"", ""read""

2. **Write operations** (create, append, patch): Describe THEN confirm
  - obsidian_create_file → ""create"", ""new file""
  - obsidian_append_to_file → ""add to"", ""append""
  - obsidian_patch_file → ""update"", ""modify"", ""change""

3. **Destructive operations** (delete, move): Show preview THEN confirm
  - obsidian_delete_file → ""delete"", ""remove""
  - obsidian_move_file → ""move"", ""relocate""

# CONFIRMATION PROTOCOL
For write/destructive operations, use this exact format:

```
I will perform the following operations:
- Create file: `path/to/file.md`
- Append to file: `another/file.md` with content: ""[summary]""

Please confirm to proceed.
```

**DO NOT execute** write/destructive operations until user confirms.

# RESPONSE GUIDELINES
- Use real emojis: ✓ ✗ 📁 📝 🔍 (not \\u codes)
- Format with Markdown: **bold**, `code`, ### headers, - lists
- When listing folders, show counts: ""📁 Projects (15 files)""
- Keep responses concise unless user asks for detail

# EXAMPLES
User: ""List all folders""
You: [Call obsidian_list_files_in_vault] → Format response with folder structure

User: ""Create a meeting note""
You: ""I will create file: `Meetings/2025-10-14-meeting.md` with template. Confirm?""

User: ""Find notes about AI""
You: [Call obsidian_search_vault with query=""AI""] → Show results with previews";
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
        bool isSuccess = !(result.IsError ?? true);

        if (result.Content?.FirstOrDefault() is ModelContextProtocol.Protocol.TextContentBlock textBlock)
        {
            responseMessage = textBlock.Text;
        }

        if (!isSuccess)
        {
            logger.LogError("MCP tool {ToolName} returned error: {Error}", op, responseMessage);
            return Results.Ok(new ModifyResponse(false, $"MCP tool error: {responseMessage}", request.FilePath));
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
