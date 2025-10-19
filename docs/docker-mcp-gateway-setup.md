# Docker MCP Gateway Configuration Guide

## Overview
The Docker MCP Gateway manages multiple MCP servers (Obsidian + Filesystem) through a single HTTP endpoint. It handles stdio↔HTTP transport translation for all servers.

## Architecture

```
Your Application (port varies)
        ↓
McpClientService
        ↓
HTTP → localhost:8033/mcp
        ↓
Docker MCP Gateway
  ├→ Obsidian Server (stdio)
  └→ Filesystem Server (stdio)
```

**Key Benefits:**
- Single endpoint for multiple MCP servers
- Gateway routes requests based on tool names
- Simplified port management
- Centralized MCP server orchestration

## Configuration Files

### Location
The Docker MCP gateway reads configuration from:
```
C:\Users\<YourUsername>\.docker\mcp\
  ├── config.yaml      (server configurations)
  └── registry.yaml    (enabled servers list)
```

### Step 1: Configure Servers

**File:** `C:\Users\<YourUsername>\.docker\mcp\config.yaml`

```yaml
servers:
  obsidian:
    env:
      OBSIDIAN_API_KEY: "your-api-key-here"
      OBSIDIAN_HOST: "host.docker.internal"
      # Add other Obsidian-specific environment variables
  
  filesystem:
    env:
      # Semicolon-separated list of allowed directories
      # IMPORTANT: Only add directories you want the AI to access
      ALLOWED_DIRECTORIES: "C:\\temp;C:\\Users\\Public\\Documents"
      
      # Example with Obsidian vault:
      # ALLOWED_DIRECTORIES: "G:\\My Drive\\obsidian-vault;C:\\temp;C:\\Users\\YourUsername\\Documents"
```

**Security Notes for ALLOWED_DIRECTORIES:**
- Use absolute Windows paths with double backslashes
- Separate multiple directories with semicolons (`;`)
- Only include directories you trust the AI to access
- Subdirectories within allowed paths are automatically accessible
- Paths outside this list are completely blocked

### Step 2: Enable Servers

**File:** `C:\Users\<YourUsername>\.docker\mcp\registry.yaml`

```yaml
enabled_servers:
  - obsidian
  - filesystem
```

This tells the gateway which servers to load when it starts.

## Aspire Configuration

The Aspire AppHost is already configured to start the gateway with both servers:

**File:** `ObsidianAI.AppHost/AppHost.cs`

```csharp
var mcpGateway = builder.AddExecutable("mcp-gateway", "docker", 
    workingDirectory: ".", 
    args: [
        "mcp", "gateway", "run",
        "--transport", "streaming",
        "--port", "8033",
        "--servers", "obsidian,filesystem"  // Both servers
    ]);
```

## Application Configuration

**File:** `ObsidianAI.Web/appsettings.json`

```json
{
  "McpServers": {
    "obsidian": {
      "Endpoint": "http://localhost:8033/mcp",
      "Enabled": true
    },
    "filesystem": {
      "Endpoint": "http://localhost:8033/mcp",
      "Enabled": true
    }
  }
}
```

**Note:** Both servers use the **same endpoint**. The gateway routes requests to the correct server based on tool names.

## How Routing Works

When `McpClientService` calls a tool:

1. **Tool Discovery Phase:**
   ```
   GET http://localhost:8033/mcp/tools
   → Gateway returns tools from ALL servers (obsidian + filesystem)
   → Each tool knows its source server
   ```

2. **Tool Execution Phase:**
   ```
   POST http://localhost:8033/mcp/tools/read_file
   → Gateway sees "read_file" is a filesystem tool
   → Routes to filesystem server
   → Returns result
   ```

The gateway maintains internal routing tables based on tool names, so your application doesn't need to know which server owns which tool.

## Setup Steps

### 1. Create Configuration Directory

```powershell
# Create Docker MCP config directory
$mcpConfigDir = "$env:USERPROFILE\.docker\mcp"
New-Item -ItemType Directory -Force -Path $mcpConfigDir
```

### 2. Create config.yaml

```powershell
# Create config.yaml
$configContent = @"
servers:
  obsidian:
    env:
      OBSIDIAN_API_KEY: "your-api-key-here"
      OBSIDIAN_HOST: "host.docker.internal"
  
  filesystem:
    env:
      ALLOWED_DIRECTORIES: "C:\\temp;C:\\Users\\Public\\Documents"
"@

Set-Content -Path "$mcpConfigDir\config.yaml" -Value $configContent
```

**⚠️ IMPORTANT:** Edit `config.yaml` and replace the paths with your actual directories!

### 3. Create registry.yaml

```powershell
# Create registry.yaml
$registryContent = @"
enabled_servers:
  - obsidian
  - filesystem
"@

Set-Content -Path "$mcpConfigDir\registry.yaml" -Value $registryContent
```

### 4. Verify Configuration

```powershell
# Check files were created
Get-ChildItem "$env:USERPROFILE\.docker\mcp"

# Should show:
# config.yaml
# registry.yaml
```

### 5. Start the Application

```powershell
cd c:\git\ObsidianAI\ObsidianAI.AppHost
dotnet run
```

**Expected behavior:**
1. Aspire starts the MCP gateway with `--servers obsidian,filesystem`
2. Gateway reads `~/.docker/mcp/config.yaml` and `registry.yaml`
3. Gateway initializes both servers
4. Web application connects to gateway
5. All tools from both servers are available

## Verification

### Check Aspire Dashboard
1. Navigate to `https://localhost:17055` (Aspire dashboard)
2. Verify `mcp-gateway` shows "Running" status
3. Check logs for successful server initialization:
   ```
   Loaded server: obsidian
   Loaded server: filesystem
   Gateway listening on http://localhost:8033
   ```

### Check Available Tools

The unified `McpClientService` will automatically discover tools from both servers:

**Expected Obsidian tools:**
- `search_vault`
- `get_note`
- `create_note`
- `update_note`
- etc.

**Expected Filesystem tools:**
- `read_file`
- `write_file`
- `list_directory`
- `create_directory`
- `move_file`
- `search_files`

### Test Tool Execution

Try filesystem operations through your chat interface:
- "List files in C:\temp"
- "Read the contents of C:\temp\test.txt"
- "Create a directory called 'notes' in C:\temp"

## Troubleshooting

### Gateway Won't Start

**Check Docker is running:**
```powershell
docker ps
```

**Check Docker MCP is installed:**
```powershell
docker mcp --version
```

**Check configuration files exist:**
```powershell
Test-Path "$env:USERPROFILE\.docker\mcp\config.yaml"
Test-Path "$env:USERPROFILE\.docker\mcp\registry.yaml"
```

### Filesystem Server Not Loading

**Check ALLOWED_DIRECTORIES format:**
- Use double backslashes: `C:\\temp`
- Use semicolons to separate: `C:\\temp;C:\\docs`
- Verify directories exist

**Check registry.yaml includes filesystem:**
```yaml
enabled_servers:
  - obsidian
  - filesystem  # ← Must be present
```

### Port Conflicts

**Check if port 8033 is available:**
```powershell
netstat -ano | findstr :8033
```

**Kill process if needed:**
```powershell
taskkill /F /PID <process_id>
```

**Or change the port** in `AppHost.cs`:
```csharp
"--port", "8035",  // Change from 8033
```
And update `appsettings.json`:
```json
"Endpoint": "http://localhost:8035/mcp"
```

### Tools Not Appearing

**Clear cache and restart:**
```powershell
# Stop all running dotnet processes
taskkill /F /IM dotnet.exe

# Start fresh
cd c:\git\ObsidianAI\ObsidianAI.AppHost
dotnet run
```

**Check logs for tool loading:**
Look for messages like:
```
Listed 15 tools from obsidian
Listed 6 tools from filesystem
```

### Permission Denied Errors

**Verify directory permissions:**
```powershell
# Check you can access the directory
Get-ChildItem "C:\temp"
```

**Ensure paths are correct:**
- Use absolute paths
- Check for typos
- Verify directories exist

## Advanced Configuration

### Environment-Specific Directories

You can create different configurations per environment:

**Development:**
```yaml
filesystem:
  env:
    ALLOWED_DIRECTORIES: "C:\\temp;C:\\dev\\scratch"
```

**Production:**
```yaml
filesystem:
  env:
    ALLOWED_DIRECTORIES: "/app/data;/app/uploads"
```

### Multiple Gateway Instances (Alternative)

If you need isolation, run separate gateways:

```csharp
// AppHost.cs
var obsidianGateway = builder.AddExecutable("obsidian-gateway", "docker",
    args: ["mcp", "gateway", "run", "--port", "8033", "--servers", "obsidian"]);

var filesystemGateway = builder.AddExecutable("filesystem-gateway", "docker",
    args: ["mcp", "gateway", "run", "--port", "8034", "--servers", "filesystem"]);
```

```json
// appsettings.json
{
  "McpServers": {
    "obsidian": {
      "Endpoint": "http://localhost:8033/mcp"
    },
    "filesystem": {
      "Endpoint": "http://localhost:8034/mcp"
    }
  }
}
```

## Security Best Practices

### Filesystem Access
1. **Principle of Least Privilege:** Only add directories absolutely needed
2. **No System Directories:** Never add C:\Windows, C:\Program Files, etc.
3. **Read-Only When Possible:** Consider read-only mounts for sensitive data
4. **Audit Access:** Monitor logs for unexpected file operations
5. **Regular Reviews:** Periodically review ALLOWED_DIRECTORIES

### API Keys
1. Store Obsidian API keys in config.yaml or environment variables
2. Never commit config.yaml with real keys to version control
3. Use different keys for dev/staging/production
4. Rotate keys regularly

### Network Security
1. Gateway runs on localhost by default (good!)
2. Don't expose port 8033 to external networks
3. Use firewall rules if needed
4. Consider VPN for remote access

## Summary

✅ **Single gateway** manages multiple MCP servers  
✅ **Shared endpoint** simplifies configuration  
✅ **Automatic routing** based on tool names  
✅ **Centralized config** in `~/.docker/mcp/`  
✅ **Security boundaries** enforced per server  
✅ **Aspire orchestration** handles lifecycle  

## Next Steps

1. **Configure your allowed directories** in `~/.docker/mcp/config.yaml`
2. **Verify configuration files** exist and are valid YAML
3. **Start the application** and check Aspire dashboard
4. **Test filesystem tools** through the chat interface
5. **Monitor logs** for successful tool execution
6. **Review security settings** and adjust as needed

## References

- **Implementation Summary:** `docs/filesystem-mcp-implementation-summary.md`
- **Setup Guide:** `docs/filesystem-mcp-setup.md`
- **Docker MCP Docs:** https://github.com/docker/mcp
- **.NET Aspire Docs:** https://learn.microsoft.com/dotnet/aspire/
