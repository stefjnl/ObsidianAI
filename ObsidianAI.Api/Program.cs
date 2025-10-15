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
    var logger = sp.GetRequiredService<ILogger<ObsidianAssistantService>>();
    var instructions = @"You help users manage their Obsidian vault.

FILE RESOLUTION STRATEGY:
When a user mentions a filename without an exact path, you MUST follow these steps:
1. Call obsidian_list_files_in_vault() to get a complete list of all file paths in the vault.
2. Normalize both the user's input and each filename from the vault for comparison. Normalization includes:
   - Removing any emoji characters (e.g., 💡, 📝, 📁, 🤖, ✨, 📓, 🔍).
   - Converting the text to lowercase.
   - Trimming leading/trailing whitespace.
   - Ensuring the filename ends with the '.md' extension (add it if missing from the user's input).
3. Compare the normalized user input against each normalized vault filename.
4. If you find a single, unambiguous match, use the original, exact vault path (including any emojis) for all subsequent operations (e.g., obsidian_append_content).
5. If you find multiple possible matches, list them for the user and ask for clarification.
6. If no matches are found, inform the user that the file does not exist and offer to create it.

NORMALIZATION EXAMPLES:
- User says: ""Project Ideas"" -> normalized: ""project ideas.md""
- Vault has: ""💡 Project Ideas.md"" -> normalized: ""project ideas.md""
- RESULT: MATCH! You will use the exact path ""💡 Project Ideas.md"" for the operation.

- User says: ""Daily note"" -> normalized: ""daily note.md""
- Vault has: ""📝 Daily Notes.md"" -> normalized: ""daily notes.md""
- RESULT: MATCH! You will use the exact path ""📝 Daily Notes.md"" for the operation.

EXAMPLE WORKFLOW:
User: ""Append 'meeting notes' to Project Ideas""

Step 1: List files
Tool: obsidian_list_files_in_vault()
Result: [""💡 Project Ideas.md"", ""📝 Daily Notes.md"", ...]

Step 2: Match filename
User input normalized: ""project ideas.md""
Check each file:
- ""💡 Project Ideas.md"" → normalized: ""project ideas.md"" ✓ MATCH
Found exact path: ""💡 Project Ideas.md""

Step 3: Append content
Tool: obsidian_append_content(path: ""💡 Project Ideas.md"", content: ""meeting notes"")

Step 4: Confirm
Response: ""I will append 'meeting notes' to the file: 💡 Project Ideas.md. Please confirm to proceed.""

CRITICAL:
- Always use the EXACT vault path (including emojis) when calling MCP tools like obsidian_append_content or obsidian_patch_content. Never strip emojis from paths passed to tools.
- To find a file by its name, you MUST use obsidian_list_files_in_vault() followed by normalization and matching. DO NOT use obsidian_simple_search or obsidian_complex_search for this purpose, as they search file contents, not filenames.
";
    return new ObsidianAssistantService(llmFactory, mcpClient, instructions, logger);
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
