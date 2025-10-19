# Docker MCP Gateway Configuration Setup Script
# This script creates the necessary configuration files for the Docker MCP gateway

param(
    [string[]]$AllowedDirectories = @("C:\temp", "C:\Users\Public\Documents"),
    [string]$ObsidianApiKey = "your-api-key-here",
    [string]$ObsidianHost = "host.docker.internal"
)

Write-Host "=== Docker MCP Gateway Configuration Setup ===" -ForegroundColor Cyan
Write-Host ""

# Define paths
$mcpConfigDir = Join-Path $env:USERPROFILE ".docker\mcp"
$configFile = Join-Path $mcpConfigDir "config.yaml"
$registryFile = Join-Path $mcpConfigDir "registry.yaml"

# Create directory if it doesn't exist
if (-not (Test-Path $mcpConfigDir)) {
    Write-Host "Creating MCP config directory: $mcpConfigDir" -ForegroundColor Yellow
    New-Item -ItemType Directory -Force -Path $mcpConfigDir | Out-Null
} else {
    Write-Host "MCP config directory exists: $mcpConfigDir" -ForegroundColor Green
}

# Convert allowed directories to YAML format (semicolon-separated with escaped backslashes)
$allowedDirsString = ($AllowedDirectories | ForEach-Object { $_.Replace('\', '\\') }) -join ';'

# Create config.yaml
Write-Host ""
Write-Host "Creating config.yaml..." -ForegroundColor Yellow

$configContent = @"
servers:
  obsidian:
    env:
      OBSIDIAN_API_KEY: "$ObsidianApiKey"
      OBSIDIAN_HOST: "$ObsidianHost"
  
  filesystem:
    env:
      # Semicolon-separated list of allowed directories
      # Only add directories you want the AI to access!
      ALLOWED_DIRECTORIES: "$allowedDirsString"
"@

Set-Content -Path $configFile -Value $configContent -Encoding UTF8
Write-Host "Created: $configFile" -ForegroundColor Green

# Create registry.yaml
Write-Host ""
Write-Host "Creating registry.yaml..." -ForegroundColor Yellow

$registryContent = @"
enabled_servers:
  - obsidian
  - filesystem
"@

Set-Content -Path $registryFile -Value $registryContent -Encoding UTF8
Write-Host "Created: $registryFile" -ForegroundColor Green

# Display configuration summary
Write-Host ""
Write-Host "=== Configuration Summary ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Enabled Servers:" -ForegroundColor Yellow
Write-Host "  - obsidian"
Write-Host "  - filesystem"
Write-Host ""
Write-Host "Obsidian Configuration:" -ForegroundColor Yellow
Write-Host "  API Key: $ObsidianApiKey"
Write-Host "  Host: $ObsidianHost"
Write-Host ""
Write-Host "Filesystem Allowed Directories:" -ForegroundColor Yellow
foreach ($dir in $AllowedDirectories) {
    $exists = Test-Path $dir
    $status = if ($exists) { "[OK]" } else { "[NOT FOUND]" }
    $color = if ($exists) { "Green" } else { "Red" }
    Write-Host "  $status $dir" -ForegroundColor $color
}

# Verify directories exist
Write-Host ""
$missingDirs = $AllowedDirectories | Where-Object { -not (Test-Path $_) }
if ($missingDirs) {
    Write-Host "WARNING: Some directories don't exist!" -ForegroundColor Red
    Write-Host "The following directories will be created or should be verified:" -ForegroundColor Yellow
    foreach ($dir in $missingDirs) {
        Write-Host "  - $dir" -ForegroundColor Yellow
    }
    Write-Host ""
    $create = Read-Host "Create missing directories? (y/n)"
    if ($create -eq 'y' -or $create -eq 'Y') {
        foreach ($dir in $missingDirs) {
            try {
                New-Item -ItemType Directory -Force -Path $dir | Out-Null
                Write-Host "Created: $dir" -ForegroundColor Green
            } catch {
                Write-Host "Failed to create: $dir - $_" -ForegroundColor Red
            }
        }
    }
}

# Display next steps
Write-Host ""
Write-Host "=== Next Steps ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Review and edit config.yaml if needed:" -ForegroundColor Yellow
Write-Host "   notepad `"$configFile`"" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Update your Obsidian API key in config.yaml" -ForegroundColor Yellow
Write-Host ""
Write-Host "3. Verify allowed directories are correct" -ForegroundColor Yellow
Write-Host ""
Write-Host "4. Start your application:" -ForegroundColor Yellow
Write-Host "   cd c:\git\ObsidianAI\ObsidianAI.AppHost" -ForegroundColor Gray
Write-Host "   dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "5. Check Aspire dashboard for 'mcp-gateway' status:" -ForegroundColor Yellow
Write-Host "   https://localhost:17055" -ForegroundColor Gray
Write-Host ""

# Offer to open config file
$openConfig = Read-Host "Open config.yaml in Notepad now? (y/n)"
if ($openConfig -eq 'y' -or $openConfig -eq 'Y') {
    notepad $configFile
}

Write-Host ""
Write-Host "Setup complete!" -ForegroundColor Green
Write-Host ""
