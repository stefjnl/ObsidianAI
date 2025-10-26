# Complete Implementation Prompt: ObsidianAI Blazor UI Redesign

## Context
You are implementing a complete UI redesign for the ObsidianAI Blazor Server application. The existing application is functional but needs a modern, polished interface. You have a working HTML/React prototype that demonstrates the exact design and all required features.

## Critical Requirements
⚠️ **PRESERVE ALL EXISTING FUNCTIONALITY** - Every feature currently working must continue to work
⚠️ **DO NOT REMOVE OR MODIFY** the backend API, Agent Framework integration, or MCP client logic
⚠️ **ONLY UPDATE** the Blazor web layer (ObsidianAI.Web project)

## Project Structure (DO NOT MODIFY)
```
ObsidianAI/
├── ObsidianAI.AppHost/          # ✅ Leave untouched
├── ObsidianAI.Api/              # ✅ Leave untouched (Agent + MCP)
└── ObsidianAI.Web/              # 🎯 THIS IS YOUR ONLY TARGET
    ├── Components/
    │   ├── Pages/
    │   │   └── Chat.razor       # Main redesign target
    │   └── Shared/
    │       ├── MessageBubble.razor    # Redesign
    │       ├── ActionCard.razor       # Redesign
    │       └── SearchResult.razor     # Keep if exists
    ├── Services/
    │   ├── IChatService.cs      # ✅ Keep interface unchanged
    │   └── ChatService.cs       # ✅ Keep implementation unchanged
    ├── Models/                  # ✅ Keep all models unchanged
    └── wwwroot/
        ├── css/chat.css         # Replace with new styles
        └── js/chat.js           # Keep scrollToBottom function
```

## Reference Materials

### 1. Current Working Implementation (PRESERVE THIS BEHAVIOR)
Located in: `/handover-15-10.md` and `/Technical-Overview-15-10-25.md`

**Key existing features that MUST continue working:**
- Message sending via `ChatService.SendMessageAndGetResponseAsync()`
- ActionCard confirmation pattern with Confirm/Cancel/Edit buttons
- File operation execution via POST `/vault/modify` endpoint
- Conversation history maintained as `List<ChatMessage>` immutable records
- Token counting per message and total
- Obsidian deep links (`obsidian://open?vault=...&file=...`)
- `@rendermode InteractiveServer` for button interactivity
- `StateHasChanged()` after updating immutable records
- `ParseResponseForComponents()` to extract ActionCards from AI responses

### 2. Target Design Reference
HTML prototype located at: `obsidian-ai-final.html` (in outputs)

## Detailed Implementation Instructions

### STEP 1: Update Chat.razor Layout Structure

**Current structure:**
```razor
@page "/"
@rendermode InteractiveServer
<!-- Fixed header, scrollable chat, fixed input -->
```

**New structure (implement exactly):**
```razor
@page "/"
@rendermode InteractiveServer

<div class="flex h-screen bg-gradient-to-br from-slate-50 to-slate-100">
    <!-- LEFT SIDEBAR -->
    <aside class="w-64 border-r bg-white/80 backdrop-blur-sm flex flex-col">
        <!-- Logo header with "New Chat" button -->
        <!-- Conversation history list -->
        <!-- Settings footer with LLM provider badge -->
    </aside>

    <!-- MAIN CHAT AREA -->
    <main class="flex-1 flex flex-col">
        <!-- Header with avatar, message count, token display, and vault/history buttons -->
        <!-- Scrollable messages area -->
        <!-- Input area with attachments, quick actions, paperclip, input, send button -->
    </main>

    <!-- RIGHT SIDEBAR (Vault Browser) - Conditional @if (vaultBrowserOpen) -->
    <aside class="w-80 border-l bg-white/80 backdrop-blur-sm flex flex-col">
        <!-- Vault tree browser -->
        <!-- File preview panel (bottom half when file selected) -->
    </aside>
</div>
```

**Key CSS classes to use:**
- `flex`, `h-screen`, `bg-gradient-to-br`, `from-slate-50`, `to-slate-100`
- `bg-white/80`, `backdrop-blur-sm` (glassmorphism effect)
- `rounded-2xl` for message bubbles
- `bg-gradient-to-br from-purple-500 to-indigo-600` for user messages and branding

### STEP 2: Implement Left Sidebar

**Components needed:**
```razor
<!-- Logo Section -->
<div class="p-4 border-b">
    <div class="flex items-center gap-2 mb-4">
        <div class="w-8 h-8 rounded-lg bg-gradient-to-br from-purple-500 to-indigo-600 flex items-center justify-center">
            <!-- Brain icon (use existing icon library or inline SVG) -->
        </div>
        <div>
            <h1 class="font-semibold text-sm">Obsidian AI</h1>
            <p class="text-xs text-slate-500">Your vault assistant</p>
        </div>
    </div>
    <button class="w-full border px-4 py-2 rounded-md text-sm">
        <!-- Plus icon --> New Chat
    </button>
</div>

<!-- Conversation History -->
<div class="flex-1 overflow-y-auto px-2 py-3">
    <!-- "Recent Conversations" button -->
    <!-- Separator -->
    <!-- "Today" section header -->
    <!-- List of conversation items -->
</div>

<!-- Settings Footer -->
<div class="p-3 border-t space-y-2">
    <div class="flex items-center justify-between text-xs mb-2">
        <span class="text-slate-600">LLM Provider</span>
        <span class="badge">@(provider == "lmstudio" ? "Local" : "Cloud")</span>
    </div>
    <button class="w-full border px-4 py-2 rounded-md text-sm">
        <!-- Settings icon --> Settings
    </button>
</div>
```

### STEP 3: Redesign Message Display (MessageBubble.razor)

**Critical: Keep all existing props/parameters intact**

```razor
@* Parameters - DO NOT CHANGE *@
@code {
    [Parameter] public ChatMessage Message { get; set; } = null!;
    [Parameter] public EventCallback<string> OnActionConfirmed { get; set; }
    [Parameter] public EventCallback<string> OnActionCancelled { get; set; }
    // ... keep all existing parameters
}

@* New layout *@
<div class="flex gap-4 @(Message.IsUser ? "justify-end" : "justify-start")">
    @if (!Message.IsUser)
    {
        <!-- AI Avatar -->
        <div class="w-8 h-8 rounded-full bg-gradient-to-br from-purple-500 to-indigo-600 flex items-center justify-center">
            <span class="text-white text-xs font-semibold">AI</span>
        </div>
    }
    
    <div class="flex-1 max-w-2xl @(Message.IsUser ? "flex justify-end" : "")">
        <!-- Message bubble -->
        <div class="rounded-2xl px-4 py-3 @(Message.IsUser ? "bg-gradient-to-br from-purple-500 to-indigo-600 text-white" : "bg-white border shadow-sm")">
            <!-- Render markdown content (use Markdig) -->
            @((MarkupString)Markdown.ToHtml(Message.Content))
            
            @if (Message.IsStreaming)
            {
                <span class="inline-block w-2 h-4 ml-1 bg-slate-400 animate-pulse"></span>
            }
            
            <!-- Attachments display -->
            @if (Message.Attachments?.Any() == true)
            {
                <div class="mt-3 space-y-2">
                    @foreach (var att in Message.Attachments)
                    {
                        <div class="flex items-center gap-2 text-xs bg-white/10 rounded px-2 py-1.5">
                            <!-- Paperclip icon -->
                            <span class="flex-1 truncate">@att.Name</span>
                            <span class="text-xs opacity-70">@FormatFileSize(att.Size)</span>
                        </div>
                    }
                </div>
            }
        </div>
        
        <!-- Token count -->
        @if (Message.Tokens != null)
        {
            <div class="text-xs text-slate-400 mt-1">
                Tokens — input: @Message.Tokens.Input.ToString("N0"), 
                output: @Message.Tokens.Output.ToString("N0"), 
                total: @Message.Tokens.Total.ToString("N0")
            </div>
        }
        
        <!-- ActionCard (if present) - KEEP EXISTING LOGIC -->
        @if (Message.ActionCard != null)
        {
            <!-- Render ActionCard component here -->
            <ActionCard Data="@Message.ActionCard" 
                       OnConfirmed="@OnActionConfirmed" 
                       OnCancelled="@OnActionCancelled" />
        }
        
        <!-- File operation result -->
        @if (Message.FileOperation != null)
        {
            <div class="mt-3">
                <button @onclick="() => OpenInObsidian(Message.FileOperation.FilePath)"
                        class="px-3 py-1.5 text-xs border rounded-md">
                    <!-- Download icon --> View in Obsidian
                </button>
            </div>
        }
        
        <!-- Timestamp -->
        <div class="text-xs text-slate-400 mt-2">
            @Message.Timestamp.ToString("HH:mm")
        </div>
    </div>
    
    @if (Message.IsUser)
    {
        <!-- User Avatar -->
        <div class="w-8 h-8 rounded-full bg-slate-200 flex items-center justify-center">
            <span class="text-slate-700 text-xs font-semibold">You</span>
        </div>
    }
</div>
```

### STEP 4: Redesign ActionCard Component

**Keep all existing event callbacks intact**

```razor
<div class="mt-3 border rounded-lg overflow-hidden bg-white">
    <div class="p-4">
        <!-- Header with title and status badge -->
        <div class="flex items-center justify-between mb-3">
            <h3 class="font-semibold text-sm flex items-center gap-2">
                <!-- Sparkles icon -->
                @Data.Title
            </h3>
            <span class="badge @GetStatusClass(Data.Status)">
                @if (Data.Status == ActionCardStatus.Processing)
                {
                    <!-- Spinner icon -->
                }
                @Data.Status.ToString()
            </span>
        </div>
        
        <!-- Action items -->
        <div class="space-y-2 mb-4">
            @for (int i = 0; i < Data.Actions.Count; i++)
            {
                var action = Data.Actions[i];
                <div class="flex items-start gap-2 text-sm">
                    <div class="w-5 h-5 rounded-full bg-slate-100 flex items-center justify-center">
                        <span class="text-xs font-medium">@(i + 1)</span>
                    </div>
                    <div class="flex-1">
                        <div class="font-medium text-slate-900">@action.Description</div>
                        <div class="text-xs text-slate-500 mt-0.5">@action.Source</div>
                    </div>
                </div>
            }
        </div>
        
        <!-- Action buttons (KEEP EXISTING LOGIC) -->
        @if (Data.Status == ActionCardStatus.Pending)
        {
            <div class="flex gap-2">
                <button @onclick="HandleConfirm" 
                        class="flex-1 bg-gradient-to-r from-green-500 to-emerald-600 text-white px-4 py-2 rounded-md text-sm">
                    <!-- CheckCircle icon --> Confirm
                </button>
                <button @onclick="HandleCancel"
                        class="flex-1 border px-4 py-2 rounded-md text-sm">
                    <!-- XCircle icon --> Cancel
                </button>
            </div>
        }
        
        <!-- Status messages -->
        @if (Data.Status == ActionCardStatus.Completed && !string.IsNullOrEmpty(Data.StatusMessage))
        {
            <div class="flex items-center gap-2 text-sm text-green-700 bg-green-50 px-3 py-2 rounded-lg">
                <!-- CheckCircle icon -->
                @Data.StatusMessage
            </div>
        }
        
        @if (Data.Status == ActionCardStatus.Failed && !string.IsNullOrEmpty(Data.StatusMessage))
        {
            <div class="flex items-center gap-2 text-sm text-red-700 bg-red-50 px-3 py-2 rounded-lg">
                <!-- AlertCircle icon -->
                @Data.StatusMessage
            </div>
        }
    </div>
</div>

@code {
    [Parameter] public ActionCardData Data { get; set; } = null!;
    [Parameter] public EventCallback<string> OnConfirmed { get; set; }
    [Parameter] public EventCallback<string> OnCancelled { get; set; }
    
    private async Task HandleConfirm()
    {
        await OnConfirmed.InvokeAsync(Data.Id);
    }
    
    private async Task HandleCancel()
    {
        await OnCancelled.InvokeAsync(Data.Id);
    }
    
    private string GetStatusClass(ActionCardStatus status) => status switch
    {
        ActionCardStatus.Completed => "bg-green-100 text-green-800",
        ActionCardStatus.Failed => "bg-red-100 text-red-800",
        ActionCardStatus.Processing => "bg-blue-100 text-blue-800",
        _ => "bg-slate-100 text-slate-800"
    };
}
```

### STEP 5: Implement Input Area with Attachments

**Add to Chat.razor:**

```razor
<!-- Input Area -->
<div class="border-t bg-white/80 backdrop-blur-sm px-6 py-4">
    <div class="max-w-3xl mx-auto">
        <!-- Attachments Preview (show only if attachments exist) -->
        @if (attachments.Any())
        {
            <div class="mb-3 flex flex-wrap gap-2">
                @foreach (var att in attachments)
                {
                    <div class="flex items-center gap-2 text-xs bg-slate-100 rounded-lg px-3 py-2 border">
                        <!-- Paperclip icon -->
                        <span class="font-medium">@att.Name</span>
                        <span class="text-slate-500">@FormatFileSize(att.Size)</span>
                        <button @onclick="() => RemoveAttachment(att.Id)" class="ml-1">
                            <!-- X icon -->
                        </button>
                    </div>
                }
            </div>
        }
        
        <!-- Quick Actions -->
        <div class="flex gap-2 mb-3 overflow-x-auto pb-2">
            <button @onclick='() => SetInputPrompt("Search my vault for ")' 
                    class="flex-shrink-0 border px-3 py-1.5 rounded-md text-xs">
                <!-- Search icon --> Search vault
            </button>
            <button @onclick='() => SetInputPrompt("Create a new note called ")' 
                    class="flex-shrink-0 border px-3 py-1.5 rounded-md text-xs">
                <!-- Plus icon --> Create note
            </button>
            <button @onclick='() => SetInputPrompt("Reorganize my vault by ")' 
                    class="flex-shrink-0 border px-3 py-1.5 rounded-md text-xs">
                <!-- FolderOpen icon --> Reorganize
            </button>
            <button @onclick='() => SetInputPrompt("Summarize the content in ")' 
                    class="flex-shrink-0 border px-3 py-1.5 rounded-md text-xs">
                <!-- FileText icon --> Summarize
            </button>
        </div>
        
        <!-- Input Row -->
        <div class="flex gap-3">
            <!-- File input (hidden) -->
            <InputFile @ref="fileInputElement" OnChange="HandleFileSelected" multiple hidden />
            
            <!-- Paperclip button -->
            <button @onclick="TriggerFileSelect" 
                    disabled="@isProcessing"
                    class="h-12 px-4 border rounded-lg">
                <!-- Paperclip icon -->
            </button>
            
            <!-- Text input -->
            <input @bind="currentMessage" 
                   @bind:event="oninput"
                   @onkeydown="HandleKeyDown"
                   disabled="@isProcessing"
                   placeholder="Ask about your vault or give me a task..."
                   class="flex-1 h-12 px-4 text-sm border rounded-lg" />
            
            <!-- Send button -->
            <button @onclick="HandleSendMessage" 
                    disabled="@(string.IsNullOrWhiteSpace(currentMessage) || isProcessing)"
                    class="h-12 px-6 bg-gradient-to-r from-purple-500 to-indigo-600 text-white rounded-lg">
                @if (isProcessing)
                {
                    <!-- Spinner icon -->
                }
                else
                {
                    <!-- Send icon --> Send
                }
            </button>
        </div>
    </div>
</div>

@code {
    private List<AttachmentData> attachments = new();
    private InputFile? fileInputElement;
    
    private void TriggerFileSelect()
    {
        // Trigger the hidden InputFile click via JS interop if needed
        // or use a more direct approach depending on your Blazor version
    }
    
    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        foreach (var file in e.GetMultipleFiles())
        {
            var att = new AttachmentData
            {
                Id = Guid.NewGuid().ToString(),
                Name = file.Name,
                Size = file.Size,
                Type = file.ContentType
            };
            attachments.Add(att);
        }
        StateHasChanged();
    }
    
    private void RemoveAttachment(string id)
    {
        attachments.RemoveAll(a => a.Id == id);
        StateHasChanged();
    }
    
    private void SetInputPrompt(string prompt)
    {
        currentMessage = prompt;
        StateHasChanged();
        // Focus input if possible
    }
    
    private string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }
}
```

### STEP 6: Implement Vault Browser Sidebar

**Create new component: VaultBrowser.razor**

```razor
<aside class="w-80 border-l bg-white/80 backdrop-blur-sm flex flex-col">
    <!-- Header -->
    <div class="p-4 border-b flex items-center justify-between">
        <h2 class="font-semibold text-sm flex items-center gap-2">
            <!-- FolderOpen icon -->
            Vault Browser
        </h2>
        <button @onclick="OnClose" class="h-6 w-6">
            <!-- X icon -->
        </button>
    </div>
    
    <!-- Tree view -->
    <div class="flex-1 overflow-y-auto px-2 py-3">
        @RenderNode(vaultRoot, 0)
    </div>
    
    <!-- File Preview Panel (bottom half, conditional) -->
    @if (selectedFile != null && selectedFile.Type == "file")
    {
        <div class="border-t bg-slate-50 p-4 flex flex-col h-1/2">
            <div class="flex items-center justify-between mb-3">
                <div class="flex items-center gap-2 min-w-0">
                    <!-- FileText icon -->
                    <h3 class="font-medium text-sm truncate">@selectedFile.Name</h3>
                </div>
                <div class="flex items-center gap-2 flex-shrink-0">
                    <button @onclick="CopyFileContent" class="px-2 py-1 text-xs border rounded">
                        @(copied ? "Copied" : "Copy")
                    </button>
                    <button @onclick="() => selectedFile = null" class="h-7 w-7">
                        <!-- X icon -->
                    </button>
                </div>
            </div>
            <div class="flex-1 bg-white rounded-lg border p-4 overflow-y-auto">
                <!-- Render markdown content with Markdig -->
                @((MarkupString)Markdown.ToHtml(selectedFile.Content ?? ""))
            </div>
        </div>
    }
</aside>

@code {
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public VaultNode VaultRoot { get; set; } = null!;
    
    private HashSet<string> expandedFolders = new() { "/" };
    private VaultNode? selectedFile;
    private bool copied = false;
    
    private RenderFragment RenderNode(VaultNode node, int depth) => builder =>
    {
        var isExpanded = expandedFolders.Contains(node.Path);
        var isSelected = selectedFile?.Path == node.Path;
        
        if (node.Type == "folder")
        {
            // Folder button
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "class", $"w-full flex items-center gap-1 px-2 py-1 text-xs hover:bg-slate-100 rounded");
            builder.AddAttribute(2, "style", $"padding-left: {depth * 12 + 8}px");
            builder.AddAttribute(3, "onclick", EventCallback.Factory.Create(this, () => ToggleFolder(node.Path)));
            
            // Chevron icon
            builder.AddContent(4, isExpanded ? "▼" : "▶");
            // Folder icon
            builder.AddContent(5, "📁 ");
            builder.AddContent(6, node.Name);
            
            builder.CloseElement();
            
            // Children
            if (isExpanded && node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    builder.AddContent(7, RenderNode(child, depth + 1));
                }
            }
        }
        else
        {
            // File button
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "class", $"w-full flex items-center gap-1 px-2 py-1 text-xs hover:bg-slate-100 rounded {(isSelected ? "bg-purple-50 border-l-2 border-purple-500" : "")}");
            builder.AddAttribute(2, "style", $"padding-left: {depth * 12 + 8}px");
            builder.AddAttribute(3, "onclick", EventCallback.Factory.Create(this, () => SelectFile(node)));
            
            // File icon
            builder.AddContent(4, "📄 ");
            builder.AddContent(5, node.Name);
            
            builder.CloseElement();
        }
    };
    
    private void ToggleFolder(string path)
    {
        if (expandedFolders.Contains(path))
            expandedFolders.Remove(path);
        else
            expandedFolders.Add(path);
        StateHasChanged();
    }
    
    private async Task SelectFile(VaultNode node)
    {
        selectedFile = node;
        // Load file content from API if not already loaded
        if (string.IsNullOrEmpty(node.Content))
        {
            // Call API to fetch file content
            // node.Content = await LoadFileContent(node.Path);
        }
        StateHasChanged();
    }
    
    private async Task CopyFileContent()
    {
        if (selectedFile?.Content != null)
        {
            // Use JS interop to copy to clipboard
            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", selectedFile.Content);
            copied = true;
            StateHasChanged();
            await Task.Delay(2000);
            copied = false;
            StateHasChanged();
        }
    }
}

// Model class
public class VaultNode
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = ""; // "file" or "folder"
    public string Path { get; set; } = "";
    public string? Content { get; set; }
    public List<VaultNode>? Children { get; set; }
}
```

### STEP 7: Update Header Section

**In Chat.razor, replace header with:**

```razor
<header class="border-b bg-white/80 backdrop-blur-sm px-6 py-3 flex items-center justify-between">
    <div class="flex items-center gap-3">
        <!-- Avatar -->
        <div class="w-8 h-8 rounded-full bg-gradient-to-br from-purple-500 to-indigo-600 flex items-center justify-center">
            <span class="text-white text-xs font-semibold">AI</span>
        </div>
        <div>
            <h2 class="font-semibold text-sm">Chat with your vault</h2>
            <p class="text-xs text-slate-500">@conversationHistory.Count messages • @selectedModel</p>
        </div>
    </div>
    <div class="flex items-center gap-4">
        <!-- Token counter -->
        <div class="text-xs text-slate-600">
            <span class="font-medium">Total tokens:</span> @totalTokens.ToString("N0")
        </div>
        <div class="flex items-center gap-2">
            <!-- Vault browser toggle -->
            <button @onclick="() => vaultBrowserOpen = !vaultBrowserOpen" 
                    class="h-8 w-8 hover:bg-slate-100 rounded">
                <!-- FolderOpen icon -->
            </button>
            <!-- History button -->
            <button class="h-8 w-8 hover:bg-slate-100 rounded">
                <!-- History icon -->
            </button>
        </div>
    </div>
</header>

@code {
    private int totalTokens = 0;
    private bool vaultBrowserOpen = false;
    private string selectedModel = "NanoGPT";
}
```

### STEP 8: Update CSS (wwwroot/css/chat.css)

**Replace entire file with:**

```css
/* Base Tailwind-inspired utility classes */
:root {
    --color-slate-50: #f8fafc;
    --color-slate-100: #f1f5f9;
    --color-slate-200: #e2e8f0;
    --color-slate-400: #94a3b8;
    --color-slate-500: #64748b;
    --color-slate-600: #475569;
    --color-slate-700: #334155;
    --color-slate-900: #0f172a;
    --color-purple-500: #a855f7;
    --color-purple-600: #9333ea;
    --color-indigo-600: #4f46e5;
    --color-green-500: #22c55e;
    --color-green-600: #16a34a;
    --color-emerald-600: #059669;
    --color-red-500: #ef4444;
    --color-red-700: #b91c1c;
}

/* Utility classes */
.flex { display: flex; }
.flex-col { flex-direction: column; }
.flex-1 { flex: 1; }
.items-center { align-items: center; }
.justify-between { justify-content: space-between; }
.justify-end { justify-content: flex-end; }
.gap-2 { gap: 0.5rem; }
.gap-3 { gap: 0.75rem; }
.gap-4 { gap: 1rem; }
.h-screen { height: 100vh; }
.w-64 { width: 16rem; }
.w-80 { width: 20rem; }
.max-w-2xl { max-width: 42rem; }
.max-w-3xl { max-width: 48rem; }
.p-3 { padding: 0.75rem; }
.p-4 { padding: 1rem; }
.px-2 { padding-left: 0.5rem; padding-right: 0.5rem; }
.px-3 { padding-left: 0.75rem; padding-right: 0.75rem; }
.px-4 { padding-left: 1rem; padding-right: 1rem; }
.px-6 { padding-left: 1.5rem; padding-right: 1.5rem; }
.py-1 { padding-top: 0.25rem; padding-bottom: 0.25rem; }
.py-2 { padding-top: 0.5rem; padding-bottom: 0.5rem; }
.py-3 { padding-top: 0.75rem; padding-bottom: 0.75rem; }
.py-4 { padding-top: 1rem; padding-bottom: 1rem; }
.border { border: 1px solid #e2e8f0; }
.border-t { border-top: 1px solid #e2e8f0; }
.border-b { border-bottom: 1px solid #e2e8f0; }
.border-l { border-left: 1px solid #e2e8f0; }
.border-r { border-right: 1px solid #e2e8f0; }
.rounded { border-radius: 0.25rem; }
.rounded-md { border-radius: 0.375rem; }
.rounded-lg { border-radius: 0.5rem; }
.rounded-2xl { border-radius: 1rem; }
.rounded-full { border-radius: 9999px; }
.shadow-sm { box-shadow: 0 1px 2px 0 rgb(0 0 0 / 0.05); }
.text-xs { font-size: 0.75rem; line-height: 1rem; }
.text-sm { font-size: 0.875rem; line-height: 1.25rem; }
.font-medium { font-weight: 500; }
.font-semibold { font-weight: 600; }
.overflow-hidden { overflow: hidden; }
.overflow-y-auto { overflow-y: auto; }
.space-y-1 > * + * { margin-top: 0.25rem; }
.space-y-2 > * + * { margin-top: 0.5rem; }
.space-y-3 > * + * { margin-top: 0.75rem; }
.space-y-6 > * + * { margin-top: 1.5rem; }

/* Gradient backgrounds */
.bg-gradient-to-br {
    background-image: linear-gradient(to bottom right, var(--tw-gradient-stops));
}
.from-slate-50 { --tw-gradient-from: var(--color-slate-50); --tw-gradient-stops: var(--tw-gradient-from), var(--tw-gradient-to, transparent); }
.to-slate-100 { --tw-gradient-to: var(--color-slate-100); }
.from-purple-500 { --tw-gradient-from: var(--color-purple-500); --tw-gradient-stops: var(--tw-gradient-from), var(--tw-gradient-to, transparent); }
.to-indigo-600 { --tw-gradient-to: var(--color-indigo-600); }
.from-green-500 { --tw-gradient-from: var(--color-green-500); --tw-gradient-stops: var(--tw-gradient-from), var(--tw-gradient-to, transparent); }
.to-emerald-600 { --tw-gradient-to: var(--color-emerald-600); }

/* Glassmorphism */
.bg-white\/80 {
    background-color: rgba(255, 255, 255, 0.8);
}
.backdrop-blur-sm {
    backdrop-filter: blur(4px);
}

/* Color utilities */
.bg-white { background-color: #ffffff; }
.bg-slate-50 { background-color: var(--color-slate-50); }
.bg-slate-100 { background-color: var(--color-slate-100); }
.bg-slate-200 { background-color: var(--color-slate-200); }
.text-white { color: #ffffff; }
.text-slate-400 { color: var(--color-slate-400); }
.text-slate-500 { color: var(--color-slate-500); }
.text-slate-600 { color: var(--color-slate-600); }
.text-slate-700 { color: var(--color-slate-700); }
.text-slate-900 { color: var(--color-slate-900); }
.text-green-700 { color: #15803d; }
.text-red-700 { color: var(--color-red-700); }
.bg-green-50 { background-color: #f0fdf4; }
.bg-red-50 { background-color: #fef2f2; }
.bg-purple-50 { background-color: #faf5ff; }
.border-purple-500 { border-color: var(--color-purple-500); }

/* Animation */
.animate-pulse {
    animation: pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite;
}
@keyframes pulse {
    0%, 100% { opacity: 1; }
    50% { opacity: 0.5; }
}

/* Markdown content styling */
.markdown-content h1 { font-size: 1.5em; font-weight: 700; margin-bottom: 0.75em; }
.markdown-content h2 { font-size: 1.3em; font-weight: 600; margin-top: 1.5em; margin-bottom: 0.5em; }
.markdown-content h3 { font-size: 1.1em; font-weight: 600; margin-top: 1.25em; margin-bottom: 0.5em; }
.markdown-content p { margin-top: 0.5em; margin-bottom: 0.5em; }
.markdown-content a { color: #2563eb; text-decoration: none; }
.markdown-content a:hover { text-decoration: underline; }
.markdown-content ul, .markdown-content ol { margin: 0.5em 0; padding-left: 1.5em; }
.markdown-content code { background-color: #f1f5f9; color: #7c3aed; padding: 0.125em 0.25em; border-radius: 0.25em; font-size: 0.875em; }
.markdown-content strong { font-weight: 600; }
.markdown-content hr { border: 0; border-top: 1px solid #e2e8f0; margin: 1.5em 0; }
```

### STEP 9: Update Models (if needed)

**Add AttachmentData model to Models folder:**

```csharp
public record AttachmentData
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public long Size { get; init; }
    public string Type { get; init; } = string.Empty;
}
```

**Update ChatMessage model to include attachments and tokens:**

```csharp
public record ChatMessage
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Content { get; init; } = string.Empty;
    public bool IsUser { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public bool IsProcessing { get; init; }
    public bool IsStreaming { get; init; }
    public ActionCardData? ActionCard { get; init; }
    public FileOperationData? FileOperation { get; init; }
    public List<AttachmentData>? Attachments { get; init; }  // NEW
    public TokenData? Tokens { get; init; }  // NEW
}

public record TokenData
{
    public int Input { get; init; }
    public int Output { get; init; }
    public int Total { get; init; }
}
```

### STEP 10: Critical Logic Preservation Checklist

**Before deploying, verify these still work:**

✅ **Message Sending:**
```csharp
var response = await ChatService.SendMessageAndGetResponseAsync(messageToSend);
```

✅ **Immutable Record Updates:**
```csharp
var index = conversationHistory.FindLastIndex(m => m.Id == currentAiMessage.Id);
conversationHistory[index] = currentAiMessage with { Content = newContent };
StateHasChanged();
```

✅ **ActionCard Parsing:**
```csharp
await ParseResponseForComponents(responseMessage.Content);
```

✅ **Action Execution:**
```csharp
var request = new ModifyRequest { Operation = operation, Path = path, Content = content };
var result = await ChatService.ModifyAsync(request);
```

✅ **Token Counting:**
```csharp
var inputTokens = (int)(message.Split(' ').Length * 1.3);
totalTokens += inputTokens;
```

✅ **Obsidian Deep Links:**
```csharp
private void OpenInObsidian(string filePath)
{
    var encoded = Uri.EscapeDataString(filePath);
    NavigationManager.NavigateTo($"obsidian://open?vault=obsidian-vault&file={encoded}", true);
}
```

## Testing Instructions

After implementation, test these critical flows:

1. **Basic Chat:** Send message → Receive response → Display correctly
2. **File Attachments:** Click paperclip → Select files → Preview chips → Send with message
3. **ActionCard Flow:** Trigger operation → See ActionCard → Click Confirm → Status updates → File operation result shows
4. **Vault Browser:** Click folder icon → Tree expands → Click file → Preview shows → Click Copy → Content copied
5. **Token Counting:** Send multiple messages → Total tokens updates → Per-message tokens display
6. **Quick Actions:** Click quick action button → Input pre-fills → Cursor focuses
7. **Responsiveness:** Resize window → Sidebars collapse gracefully → Chat remains usable

## Common Pitfalls to Avoid

❌ **DO NOT** change the API endpoint signatures
❌ **DO NOT** modify how ChatService communicates with the backend
❌ **DO NOT** remove `@rendermode InteractiveServer`
❌ **DO NOT** forget `StateHasChanged()` after updating immutable records
❌ **DO NOT** change the ActionCard confirmation pattern
❌ **DO NOT** remove emoji support from file paths
❌ **DO NOT** modify the MCP tool calling logic
❌ **DO NOT** change how conversation history is maintained

## Success Criteria

✅ All existing features work exactly as before
✅ New UI matches the HTML prototype design
✅ Token counting displays correctly
✅ File attachments can be added and removed
✅ Vault browser opens/closes and displays tree
✅ File preview shows markdown rendered content
✅ Copy button works in file preview
✅ ActionCards trigger and execute correctly
✅ Messages display with proper styling and avatars
✅ Quick action buttons pre-fill input
✅ Application compiles without errors
✅ No console errors in browser

## Final Notes

This is a **visual redesign only**. The entire backend architecture (Aspire, Agent Framework, MCP integration) remains untouched. You're only updating the Blazor web layer to match the modern design while preserving every single piece of existing functionality.

If any feature doesn't work after implementation, revert that change and consult the handover documentation for the correct pattern.
