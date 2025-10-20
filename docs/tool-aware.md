## Dynamic Tool Selection Strategy

You need **context-aware tool loading** - only send relevant tools based on the user's query intent.

---

## Solution: Tool Selection Middleware

### Architecture

```
User Query
    ↓
Intent Analyzer → Determines which MCP servers are needed
    ↓
Tool Catalog → Loads ONLY tools from relevant servers
    ↓
Agent → Gets filtered tool list (stays under API limit)
```

---

## Implementation

### Step 1: Add Intent Analysis

**File:** `ObsidianAI.Application/Services/IToolSelectionStrategy.cs`

```csharp
namespace ObsidianAI.Application.Services;

public interface IToolSelectionStrategy
{
    Task<IEnumerable<string>> SelectServersForQueryAsync(
        string query, 
        CancellationToken cancellationToken = default);
}

public class KeywordBasedToolSelectionStrategy : IToolSelectionStrategy
{
    private readonly ILogger<KeywordBasedToolSelectionStrategy> _logger;

    public KeywordBasedToolSelectionStrategy(ILogger<KeywordBasedToolSelectionStrategy> logger)
    {
        _logger = logger;
    }

    public Task<IEnumerable<string>> SelectServersForQueryAsync(
        string query, 
        CancellationToken cancellationToken = default)
    {
        var lowerQuery = query.ToLowerInvariant();
        var selectedServers = new List<string>();

        // Always include Obsidian (primary use case)
        selectedServers.Add("obsidian");

        // Filesystem keywords
        if (ContainsFileSystemIntent(lowerQuery))
        {
            selectedServers.Add("filesystem");
            _logger.LogInformation("Filesystem tools selected based on query intent");
        }

        // Microsoft Learn keywords
        if (ContainsMicrosoftLearnIntent(lowerQuery))
        {
            selectedServers.Add("microsoft-learn");
            _logger.LogInformation("Microsoft Learn tools selected based on query intent");
        }

        _logger.LogInformation("Selected {Count} MCP servers: {Servers}", 
            selectedServers.Count, 
            string.Join(", ", selectedServers));

        return Task.FromResult<IEnumerable<string>>(selectedServers);
    }

    private static bool ContainsFileSystemIntent(string query)
    {
        var fileSystemKeywords = new[]
        {
            "file", "folder", "directory", "document", "documents folder",
            "temp", "desktop", "download", "path", "read file", "write file",
            "create file", "delete file", "move file", "copy file", "list files",
            "c:\\", "g:\\", ".txt", ".pdf", ".docx", ".xlsx"
        };

        return fileSystemKeywords.Any(keyword => query.Contains(keyword));
    }

    private static bool ContainsMicrosoftLearnIntent(string query)
    {
        var msLearnKeywords = new[]
        {
            "documentation", "docs", "microsoft", ".net", "c#", "csharp",
            "azure", "asp.net", "blazor", "entity framework", "learn",
            "tutorial", "api reference", "guide"
        };

        return msLearnKeywords.Any(keyword => query.Contains(keyword));
    }
}
```

### Step 2: Update Tool Catalog with Filtering

**File:** `ObsidianAI.Application/Services/IMcpToolCatalog.cs`

```csharp
public interface IMcpToolCatalog
{
    Task<IEnumerable<McpTool>> GetAllToolsAsync(CancellationToken cancellationToken = default);
    
    Task<IEnumerable<McpTool>> GetToolsByServerAsync(
        string serverName, 
        CancellationToken cancellationToken = default);
    
    // NEW: Get tools from multiple specific servers
    Task<IEnumerable<McpTool>> GetToolsFromServersAsync(
        IEnumerable<string> serverNames, 
        CancellationToken cancellationToken = default);
    
    void InvalidateCache();
}
```

**File:** `ObsidianAI.Application/Services/McpToolCatalog.cs`

```csharp
public async Task<IEnumerable<McpTool>> GetToolsFromServersAsync(
    IEnumerable<string> serverNames, 
    CancellationToken cancellationToken = default)
{
    var serverList = serverNames.ToList();
    if (serverList.Count == 0)
    {
        _logger.LogWarning("No servers specified for tool fetching");
        return Enumerable.Empty<McpTool>();
    }

    _logger.LogInformation("Fetching tools from {Count} servers: {Servers}", 
        serverList.Count, 
        string.Join(", ", serverList));

    var fetchTasks = serverList.Select(async serverName =>
    {
        try
        {
            var tools = await _clientProvider.ListToolsAsync(serverName, cancellationToken);
            var mcpTools = tools.Select(t => new McpTool
            {
                ServerName = serverName,
                Tool = t
            }).ToList();

            _logger.LogDebug("Fetched {Count} tools from {ServerName}", 
                mcpTools.Count, serverName);
            return mcpTools.AsEnumerable();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch tools from {ServerName}", serverName);
            return Enumerable.Empty<McpTool>();
        }
    });

    var results = await Task.WhenAll(fetchTasks);
    var allTools = results.SelectMany(r => r).ToList();

    _logger.LogInformation("Fetched {TotalCount} tools from {ServerCount} servers", 
        allTools.Count, serverList.Count);

    return allTools;
}
```

### Step 3: Update Use Cases to Use Intent-Based Selection

**File:** `ObsidianAI.Application/UseCases/StreamChatUseCase.cs`

```csharp
public class StreamChatUseCase
{
    private readonly IMcpToolCatalog _toolCatalog;
    private readonly IToolSelectionStrategy _toolSelection;
    private readonly IChatAgent _agent;
    private readonly ILogger<StreamChatUseCase> _logger;

    public StreamChatUseCase(
        IMcpToolCatalog toolCatalog,
        IToolSelectionStrategy toolSelection,
        IChatAgent agent,
        ILogger<StreamChatUseCase> logger)
    {
        _toolCatalog = toolCatalog;
        _toolSelection = toolSelection;
        _agent = agent;
        _logger = logger;
    }

    public async Task<IAsyncEnumerable<ChatStreamEvent>> ExecuteAsync(
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Analyze query to determine needed servers
        var selectedServers = await _toolSelection.SelectServersForQueryAsync(
            userMessage, 
            cancellationToken);

        // Step 2: Load ONLY tools from selected servers
        var tools = await _toolCatalog.GetToolsFromServersAsync(
            selectedServers, 
            cancellationToken);

        _logger.LogInformation(
            "Query: '{Query}' → Selected {ServerCount} servers, loaded {ToolCount} tools",
            userMessage, 
            selectedServers.Count(), 
            tools.Count());

        // Step 3: Create agent with filtered tools
        var agentTools = tools.Select(t => t.Tool).Cast<AITool>().ToList();
        
        // Stream response...
        return _agent.RunStreamingAsync(userMessage, agentTools, cancellationToken);
    }
}
```

### Step 4: Register Services

**File:** `ObsidianAI.Infrastructure/DependencyInjection.cs`

```csharp
public static IServiceCollection AddMcpServices(
    this IServiceCollection services, 
    IConfiguration configuration)
{
    services.AddSingleton<IMcpClientProvider, McpClientService>();
    services.AddSingleton<IMcpToolCatalog, McpToolCatalog>();
    
    // NEW: Tool selection strategy
    services.AddSingleton<IToolSelectionStrategy, KeywordBasedToolSelectionStrategy>();
    
    services.AddHostedService<McpClientWarmupService>();
    
    services.AddHealthChecks()
        .AddCheck<McpHealthCheck>("mcp", tags: new[] { "ready", "mcp" });
    
    return services;
}
```

---

## Example Scenarios

### Scenario 1: Obsidian-only query
```
User: "What's in my vault?"
→ Servers: [obsidian]
→ Tools: 12 obsidian tools
→ ✅ Under API limit
```

### Scenario 2: Filesystem query
```
User: "Read the file in my Documents folder"
→ Servers: [obsidian, filesystem]
→ Tools: 12 obsidian + 12 filesystem = 24 tools
→ ✅ Under API limit (or configure fallback - see below)
```

### Scenario 3: Documentation query
```
User: "Show me .NET documentation about Blazor"
→ Servers: [obsidian, microsoft-learn]
→ Tools: 12 obsidian + 3 microsoft-learn = 15 tools
→ ✅ Under API limit
```

---

## Advanced: Fallback Strategy if Still Over Limit

### Step 5: Add Tool Count Limit

**File:** `ObsidianAI.Application/Services/KeywordBasedToolSelectionStrategy.cs`

```csharp
public class KeywordBasedToolSelectionStrategy : IToolSelectionStrategy
{
    private const int MaxToolsPerRequest = 20; // OpenRouter limit
    private readonly IMcpToolCatalog _toolCatalog;

    public async Task<IEnumerable<string>> SelectServersForQueryAsync(
        string query, 
        CancellationToken cancellationToken = default)
    {
        var candidates = new List<string> { "obsidian" }; // Always include primary

        var lowerQuery = query.ToLowerInvariant();
        
        if (ContainsFileSystemIntent(lowerQuery))
            candidates.Add("filesystem");
            
        if (ContainsMicrosoftLearnIntent(lowerQuery))
            candidates.Add("microsoft-learn");

        // Check if combined tools exceed limit
        var toolCount = 0;
        var selectedServers = new List<string>();

        foreach (var server in candidates)
        {
            var serverTools = await _toolCatalog.GetToolsByServerAsync(server, cancellationToken);
            var serverToolCount = serverTools.Count();

            if (toolCount + serverToolCount <= MaxToolsPerRequest)
            {
                selectedServers.Add(server);
                toolCount += serverToolCount;
            }
            else
            {
                _logger.LogWarning(
                    "Skipping {Server}: would exceed tool limit ({Current} + {Additional} > {Max})",
                    server, toolCount, serverToolCount, MaxToolsPerRequest);
                break;
            }
        }

        return selectedServers;
    }
}
```

---

## Configuration-Based Override

Allow users to manually force specific servers:

**File:** `appsettings.json`

```json
{
  "McpServers": {
    "obsidian": {
      "Endpoint": "http://localhost:8033/mcp",
      "Enabled": true,
      "AlwaysInclude": true
    },
    "filesystem": {
      "Endpoint": "http://localhost:8033/mcp",
      "Enabled": true,
      "AlwaysInclude": false
    },
    "microsoft-learn": {
      "Endpoint": "https://learn.microsoft.com/api/mcp",
      "Enabled": true,
      "AlwaysInclude": false
    }
  },
  "ToolSelection": {
    "Strategy": "Keyword",  // Options: "Keyword", "All", "Manual"
    "MaxToolsPerRequest": 20
  }
}
```

---

## Alternative: LLM-Based Intent Analysis (Future Enhancement)

For more sophisticated detection:

```csharp
public class LlmBasedToolSelectionStrategy : IToolSelectionStrategy
{
    public async Task<IEnumerable<string>> SelectServersForQueryAsync(
        string query, 
        CancellationToken cancellationToken)
    {
        // Use lightweight LLM call to classify intent
        var prompt = $"""
            Classify this user query into categories:
            - obsidian: Queries about notes/vault
            - filesystem: Queries about computer files/folders
            - microsoft-learn: Queries about .NET/Microsoft documentation
            
            Query: "{query}"
            
            Return only comma-separated categories needed.
            """;

        var response = await _lightweightLlm.CompleteAsync(prompt);
        var servers = response.Split(',').Select(s => s.Trim()).ToList();
        
        return servers;
    }
}
```

---

## Summary

**Benefits:**
- ✅ Stays under OpenRouter's 20-tool limit
- ✅ All 3 MCP servers available when needed
- ✅ No manual configuration per query
- ✅ Extensible for future MCP servers