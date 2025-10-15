## Critical Issue #1: Agent Tool Selection Failure

**Root Cause:** Your agent instructions are vague and don't establish clear decision rules for tool selection.

### Current Instructions (Problematic)
```csharp
var instructions = @"You help users manage their Obsidian vault.

RULES:
- Simple file creation (empty or with specified content): Execute directly, confirm after
- File modification (append/update existing): Ask for confirmation FIRST
- Bulk operations (move multiple files): ALWAYS show preview with confirmation
- Destructive operations (delete): ALWAYS require explicit confirmation

Be efficient. Don't ask unnecessary questions.";
```

**Problems:**
1. No explicit tool selection guidance
2. Contradictory rules ("be efficient" vs "ask for confirmation FIRST")
3. Missing examples of tool-to-task mapping
4. No instruction to analyze user intent before acting

### Fixed Instructions

```csharp
var instructions = @"You are an Obsidian vault assistant. Always analyze the user's request to determine the correct tool.

TOOL SELECTION RULES:
- List/browse requests (""show folders"", ""list files"") → use obsidian_list_files_in_vault or obsidian_read_file
- Search queries (""find notes about..."") → use obsidian_search_vault
- File creation (""create note"", ""new file"") → use obsidian_create_file
- File modification (""append to"", ""update"", ""add to"") → use obsidian_append_to_file or obsidian_patch_file
- File operations (""move"", ""delete"", ""rename"") → use appropriate file operation tool

CONFIRMATION PROTOCOL:
1. Read-only operations (list, search, read): Execute immediately, no confirmation needed
2. Simple file creation with specified content: Execute immediately, confirm after
3. File modifications (append, update, patch): Describe planned changes, wait for user confirmation
4. Destructive operations (delete, move multiple files): Show detailed preview, require explicit confirmation
5. Bulk operations (>3 files): Always show preview with file list

RESPONSE FORMAT:
- Use actual emojis (✓ ✗ 📁 📝) not Unicode escapes
- Format responses with Markdown (headers, lists, code blocks)
- For confirmations, use this exact format:
  I will perform the following operations:
  - [operation description]
  - [operation description]
  
  Please confirm to proceed.

When listing folders, return a formatted list with folder names and file counts.
When uncertain which tool to use, explain your reasoning before choosing.";
```

**Why This Works:**
- Explicit tool-to-intent mapping
- Clear decision tree for confirmations
- Reduces ambiguity about "efficiency"
- Establishes response format expectations

---

## Critical Issue #2: Unicode Escape Sequences

**Root Cause:** The agent is outputting escaped Unicode, and you're rendering it raw.

### Solution: Add Unicode Decoder Utility

Create `ObsidianAI.Web/Services/TextDecoderService.cs`:

```csharp
using System.Text.RegularExpressions;

namespace ObsidianAI.Web.Services;

public static class TextDecoderService
{
    private static readonly Regex UnicodeRegex = new(@"\\u([0-9A-Fa-f]{4})", RegexOptions.Compiled);

    public static string DecodeUnicode(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return UnicodeRegex.Replace(text, match =>
        {
            var hex = match.Groups[1].Value;
            var codePoint = Convert.ToInt32(hex, 16);
            return char.ConvertFromUtf32(codePoint);
        });
    }

    public static string DecodeSurrogatePairs(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Handle surrogate pairs like \ud83d\udca1
        var result = UnicodeRegex.Replace(text, match =>
        {
            var hex = match.Groups[1].Value;
            var value = Convert.ToInt32(hex, 16);
            return char.ConvertFromUtf32(value);
        });

        // Combine surrogate pairs
        var chars = result.ToCharArray();
        var builder = new System.Text.StringBuilder();
        
        for (int i = 0; i < chars.Length; i++)
        {
            if (char.IsHighSurrogate(chars[i]) && i + 1 < chars.Length && char.IsLowSurrogate(chars[i + 1]))
            {
                var codePoint = char.ConvertToUtf32(chars[i], chars[i + 1]);
                builder.Append(char.ConvertFromUtf32(codePoint));
                i++; // Skip the low surrogate
            }
            else
            {
                builder.Append(chars[i]);
            }
        }

        return builder.ToString();
    }
}
```

### Apply in SignalR Hub

Update `ObsidianAI.Web/Hubs/ChatHub.cs`:

```csharp
public async Task StreamMessage(string message)
{
    _logger.LogInformation("Processing message: {Message}", message);

    try
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/chat/stream")
        {
            Content = JsonContent.Create(new { message })
        };

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        var fullResponse = new StringBuilder();
        var buffer = new char[128];
        int charsRead;

        while ((charsRead = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            var chunk = new string(buffer, 0, charsRead);
            
            // DECODE UNICODE BEFORE SENDING
            var decodedChunk = TextDecoderService.DecodeSurrogatePairs(chunk);
            fullResponse.Append(decodedChunk);

            await Clients.Caller.SendAsync("ReceiveToken", decodedChunk);
            await Task.Delay(10);
        }

        var finalResponse = TextDecoderService.DecodeSurrogatePairs(fullResponse.ToString());
        await Clients.Caller.SendAsync("MessageComplete", finalResponse);

        _logger.LogInformation("Message processing complete");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing message");
        await Clients.Caller.SendAsync("Error", $"Failed to process message: {ex.Message}");
    }
}
```

---

## Critical Issue #3: Missing Markdown Rendering

### Add Markdig Package

Update `ObsidianAI.Web/ObsidianAI.Web.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Markdig" Version="0.37.0" />
</ItemGroup>
```

### Create Markdown Component

Create `ObsidianAI.Web/Components/Shared/MarkdownContent.razor`:

```razor
@using Markdig
@inject IJSRuntime JSRuntime

<div class="markdown-content" @ref="markdownElement">
    @((MarkupString)HtmlContent)
</div>

@code {
    [Parameter]
    public string Content { get; set; } = string.Empty;

    private ElementReference markdownElement;
    private string HtmlContent => ConvertMarkdown(Content);

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseSoftlineBreakAsHardlineBreak()
        .Build();

    private static string ConvertMarkdown(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        var html = Markdown.ToHtml(markdown, Pipeline);
        return html;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JSRuntime.InvokeVoidAsync("highlightCode", markdownElement);
        }
    }
}
```

### Update MessageBubble Component

Replace content rendering in `ObsidianAI.Web/Components/Shared/MessageBubble.razor`:

```razor
@using ObsidianAI.Web.Models

<div class="message @(Message.Sender == MessageSender.User ? "user" : "")">
    <div class="message-avatar" aria-hidden="true">
        @(Message.Sender == MessageSender.User ? "👤" : "🤖")
    </div>
    <div class="message-content">
        @if (Message.Sender == MessageSender.AI)
        {
            <MarkdownContent Content="@Message.Content" />
        }
        else
        {
            @Message.Content
        }
        
        @if (Message.IsProcessing)
        {
            <div class="status-indicator">
                <div class="spinner"></div>
                @GetProcessingText(Message.ProcessingType)
            </div>
        }
        
        @if (Message.ActionCard != null)
        {
            <ActionCard Card="Message.ActionCard" OnConfirmed="OnActionConfirmed" OnCancelled="OnActionCancelled" OnEdit="OnActionEdit" />
        }
        
        @if (Message.SearchResults.Any())
        {
            @foreach (var result in Message.SearchResults)
            {
                <SearchResult Result="result" />
            }
        }
    </div>
</div>

@code {
    [Parameter]
    public ChatMessage Message { get; set; } = default!;
    
    [Parameter]
    public EventCallback<string> OnActionConfirmed { get; set; }
    
    [Parameter]
    public EventCallback<string> OnActionCancelled { get; set; }
    
    [Parameter]
    public EventCallback<string> OnActionEdit { get; set; }
    
    private string GetProcessingText(ProcessingType processingType)
    {
        return processingType switch
        {
            ProcessingType.Searching => "🔍 Searching...",
            ProcessingType.Writing => "✏️ Writing...",
            ProcessingType.Reorganizing => "🔄 Reorganizing...",
            ProcessingType.Thinking => "🤔 Thinking...",
            _ => "Processing..."
        };
    }
}
```

### Add Markdown CSS

Add to `ObsidianAI.Web/wwwroot/css/chat.css`:

```css
.markdown-content {
    line-height: 1.6;
}

.markdown-content h1,
.markdown-content h2,
.markdown-content h3 {
    margin-top: 1.5rem;
    margin-bottom: 0.75rem;
    font-weight: 600;
}

.markdown-content h1 { font-size: 1.5rem; }
.markdown-content h2 { font-size: 1.25rem; }
.markdown-content h3 { font-size: 1.1rem; }

.markdown-content ul,
.markdown-content ol {
    margin-left: 1.5rem;
    margin-bottom: 1rem;
}

.markdown-content li {
    margin-bottom: 0.25rem;
}

.markdown-content code {
    background-color: #f5f5f5;
    padding: 0.2rem 0.4rem;
    border-radius: 3px;
    font-family: 'Courier New', monospace;
    font-size: 0.9em;
}

.markdown-content pre {
    background-color: #f5f5f5;
    padding: 1rem;
    border-radius: 5px;
    overflow-x: auto;
    margin-bottom: 1rem;
}

.markdown-content pre code {
    background: none;
    padding: 0;
}

.markdown-content blockquote {
    border-left: 4px solid #ddd;
    padding-left: 1rem;
    margin-left: 0;
    color: #666;
    font-style: italic;
}
```

---

## Critical Issue #4: Expand Confirmation Parser

### Replace ParseResponseForComponents in Chat.razor

```csharp
private Task ParseResponseForComponents(string response)
{
    var actions = new List<PlannedAction>();
    var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);

    // Detection patterns
    bool hasConfirmation = ContainsConfirmationKeywords(response);
    bool hasFileOperation = false;

    foreach (var line in lines)
    {
        var trimmed = line.Trim();
        var lower = trimmed.ToLowerInvariant();

        // Skip empty or very short lines
        if (trimmed.Length < 5) continue;

        // Detect operation type and extract details
        PlannedAction? action = null;

        // MOVE operations
        if ((lower.Contains("move") || trimmed.Contains("→")) && ExtractPaths(trimmed, out var movePaths) && movePaths.Count >= 2)
        {
            action = new PlannedAction
            {
                Icon = "📦",
                Description = trimmed,
                Type = ActionType.Move,
                Source = movePaths[0],
                Destination = movePaths[1]
            };
            hasFileOperation = true;
        }
        // DELETE operations
        else if (lower.Contains("delete") || lower.Contains("remove"))
        {
            var paths = ExtractAllPaths(trimmed);
            if (paths.Any())
            {
                action = new PlannedAction
                {
                    Icon = "🗑️",
                    Description = trimmed,
                    Type = ActionType.Delete,
                    Source = paths.First()
                };
                hasFileOperation = true;
            }
        }
        // APPEND operations
        else if (lower.Contains("append") || lower.Contains("add to"))
        {
            var paths = ExtractAllPaths(trimmed);
            if (paths.Any())
            {
                action = new PlannedAction
                {
                    Icon = "➕",
                    Description = trimmed,
                    Type = ActionType.Other,
                    Source = paths.First()
                };
                hasFileOperation = true;
            }
        }
        // CREATE operations
        else if (lower.Contains("create") || lower.Contains("new file"))
        {
            var paths = ExtractAllPaths(trimmed);
            if (paths.Any())
            {
                action = new PlannedAction
                {
                    Icon = "🆕",
                    Description = trimmed,
                    Type = ActionType.Create,
                    Source = paths.First()
                };
                hasFileOperation = true;
            }
        }
        // WRITE/UPDATE/MODIFY operations
        else if (lower.Contains("write") || lower.Contains("update") || lower.Contains("modify") || lower.Contains("patch"))
        {
            var paths = ExtractAllPaths(trimmed);
            if (paths.Any())
            {
                action = new PlannedAction
                {
                    Icon = "✍️",
                    Description = trimmed,
                    Type = ActionType.Other,
                    Source = paths.First()
                };
                hasFileOperation = true;
            }
        }
        // RENAME operations
        else if (lower.Contains("rename"))
        {
            var paths = ExtractAllPaths(trimmed);
            if (paths.Count >= 2)
            {
                action = new PlannedAction
                {
                    Icon = "✏️",
                    Description = trimmed,
                    Type = ActionType.Rename,
                    Source = paths[0],
                    Destination = paths[1]
                };
                hasFileOperation = true;
            }
        }

        if (action != null)
        {
            actions.Add(action);
        }
    }

    // Only create ActionCard if we detected file operations AND confirmation keywords
    if (hasFileOperation && hasConfirmation && actions.Any())
    {
        var opType = DetermineOperationType(actions);

        var actionCard = new ActionCardData
        {
            Title = "Planned File Operations",
            Actions = actions,
            HasMoreActions = actions.Count > 3,
            HiddenActionCount = Math.Max(0, actions.Count - 3),
            Status = ActionCardStatus.Pending,
            OperationType = opType
        };

        if (currentAiMessage != null)
        {
            var index = conversationHistory.IndexOf(currentAiMessage);
            if (index >= 0)
            {
                currentAiMessage = currentAiMessage with 
                { 
                    ActionCard = actionCard, 
                    ProcessingType = ProcessingType.None 
                };
                conversationHistory[index] = currentAiMessage;
            }
        }
    }

    return Task.CompletedTask;
}

private static bool ContainsConfirmationKeywords(string text)
{
    var lower = text.ToLowerInvariant();
    return lower.Contains("confirm") ||
           lower.Contains("confirmation") ||
           lower.Contains("proceed") ||
           lower.Contains("are you sure") ||
           lower.Contains("please verify") ||
           lower.Contains("i will perform") ||
           lower.Contains("planned operations");
}

private static ActionOperationType DetermineOperationType(List<PlannedAction> actions)
{
    if (actions.All(a => a.Type == ActionType.Move))
        return ActionOperationType.Move;
    if (actions.All(a => a.Type == ActionType.Delete))
        return ActionOperationType.Delete;
    if (actions.All(a => a.Type == ActionType.Create))
        return ActionOperationType.Create;
    if (actions.Count > 3)
        return ActionOperationType.Reorganize;
    return ActionOperationType.Other;
}

private static bool ExtractPaths(string text, out List<string> paths)
{
    paths = ExtractAllPaths(text);
    return paths.Count >= 2;
}

private static List<string> ExtractAllPaths(string text)
{
    var results = new HashSet<string>();

    // Pattern 1: Quoted paths
    var quotedMatches = Regex.Matches(text, @"""([^""]+\.[a-zA-Z0-9]+)""");
    foreach (Match m in quotedMatches)
    {
        results.Add(m.Groups[1].Value);
    }

    // Pattern 2: Backtick paths
    var backtickMatches = Regex.Matches(text, @"`([^`]+\.[a-zA-Z0-9]+)`");
    foreach (Match m in backtickMatches)
    {
        results.Add(m.Groups[1].Value);
    }

    // Pattern 3: Paths with slashes and extensions
    var slashMatches = Regex.Matches(text, @"([\w\-./\\]+\.[a-zA-Z0-9]+)");
    foreach (Match m in slashMatches)
    {
        var path = m.Groups[1].Value.Trim();
        if (!path.StartsWith("http") && !path.StartsWith("www"))
        {
            results.Add(path);
        }
    }

    return results.ToList();
}
```

---

## Critical Issue #5: Agent Behavior Optimization

### Update Program.cs with Refined Instructions

Replace the agent initialization in `ObsidianAI.Api/Program.cs`:

```csharp
builder.Services.AddSingleton<ObsidianAssistantService>(sp =>
{
    var llmFactory = sp.GetRequiredService<ILlmClientFactory>();
    var mcpClient = sp.GetRequiredService<McpClient>();
    
    var instructions = @"You are an Obsidian vault assistant with access to file management tools.

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
```

---

## Why Agent Chooses Wrong Tools

**Analysis of Original Failure:**

1. **No Tool-Intent Mapping:** Instructions never explicitly said "when user says X, use tool Y"
2. **Ambiguous Rules:** "Be efficient" contradicted "ask for confirmation FIRST"
3. **No Examples:** Agent had no reference for correct tool usage
4. **Generic Phrases:** "help users manage" is too vague for tool selection

**Microsoft Agent Framework Behavior:**
- Uses instruction context + tool descriptions to select tools
- Without explicit guidance, relies on tool names/descriptions alone
- LM Studio models (especially smaller ones) need stronger prompting
- Framework doesn't retry if wrong tool chosen - requires explicit instructions

**Testing the Fix:**

After implementing these changes, test with:
```
1. "List all folders in my vault" → Should call obsidian_list_files_in_vault
2. "Append 'test' to notes.md" → Should show confirmation ActionCard
3. "Create daily-note.md" → Should execute immediately, confirm after
4. "Delete old-project/" → Should show detailed preview with confirmation
```

Monitor logs for tool calls: `_logger.LogInformation("Tool selected: {ToolName}", toolName)` in ObsidianAssistantService.