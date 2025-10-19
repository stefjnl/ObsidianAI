# Deployment Secrets Configuration Guide

**Project:** ObsidianAI  
**Last Updated:** October 18, 2025

## Overview

ObsidianAI uses **User Secrets** for local development and supports **environment variables** for production deployments. API keys and sensitive configuration are never committed to source control.

---

## Table of Contents

1. [Required Secrets](#required-secrets)
2. [Local Development Setup](#local-development-setup)
3. [Production Deployment](#production-deployment)
4. [Azure Deployment](#azure-deployment)
5. [Docker Deployment](#docker-deployment)
6. [Verification](#verification)
7. [Troubleshooting](#troubleshooting)

---

## Required Secrets

### API Project (`ObsidianAI.Api`)

The API requires LLM provider credentials based on which provider you're using:

| Secret Key | Required For | Example Value | Description |
|------------|--------------|---------------|-------------|
| `LLM:OpenRouter:ApiKey` | OpenRouter | `sk-or-v1-xxxx...` | OpenRouter API key for AI model access |
| `LLM:LMStudio:ApiKey` | LM Studio | `lm-studio` | LM Studio API key (can be any value for local) |
| `LLM:NanoGPT:ApiKey` | NanoGPT | `ngpt-xxxx...` | NanoGPT API key for AI model access |

**User Secrets ID:** `obsidianai-api-secrets`

### AppHost Project (`ObsidianAI.AppHost`)

The AppHost orchestration project requires:

| Secret Key | Required | Example Value | Description |
|------------|----------|---------------|-------------|
| `LLM:OpenRouter:ApiKey` | Yes (if using OpenRouter) | `sk-or-v1-xxxx...` | Same as API project |
| `AppHost:OtlpApiKey` | Optional | `2307a09...` | OpenTelemetry collector API key |
| `Aspire:VersionCheck:LastCheckDate` | Auto-generated | `2025-10-16T16:01:31...` | Aspire version check timestamp |

**User Secrets ID:** `6b4cb36c-20cf-4195-ad75-eac95568ef40`

---

## Local Development Setup

### Prerequisites

- .NET 9.0 SDK or later
- Visual Studio 2022 or VS Code with C# Dev Kit

### Step 1: Initialize User Secrets

Run these commands from the repository root:

```powershell
# For API project
cd c:\git\ObsidianAI
dotnet user-secrets init --project ObsidianAI.Api

# For AppHost project (Aspire orchestration)
dotnet user-secrets init --project ObsidianAI.AppHost
```

### Step 2: Configure Secrets

#### For OpenRouter (Recommended for Production-Like Testing)

```powershell
# API project
dotnet user-secrets set "LLM:OpenRouter:ApiKey" "your-openrouter-api-key-here" --project ObsidianAI.Api

# AppHost project (needed for Aspire orchestration)
dotnet user-secrets set "LLM:OpenRouter:ApiKey" "your-openrouter-api-key-here" --project ObsidianAI.AppHost
```

**Getting an OpenRouter API Key:**
1. Go to [https://openrouter.ai](https://openrouter.ai)
2. Sign up for an account
3. Navigate to "Keys" section
4. Generate a new API key
5. Copy the key (starts with `sk-or-v1-`)

#### For LM Studio (Local Development Only)

```powershell
# API project
dotnet user-secrets set "LLM:LMStudio:ApiKey" "lm-studio" --project ObsidianAI.Api

# AppHost project
dotnet user-secrets set "LLM:LMStudio:ApiKey" "lm-studio" --project ObsidianAI.AppHost
```

#### For NanoGPT

```powershell
# API project
dotnet user-secrets set "LLM:NanoGPT:ApiKey" "your-nanogpt-api-key-here" --project ObsidianAI.Api

# AppHost project (needed for Aspire orchestration)
dotnet user-secrets set "LLM:NanoGPT:ApiKey" "your-nanogpt-api-key-here" --project ObsidianAI.AppHost
```

**Getting a NanoGPT API Key:**
1. Go to [https://docs.nano-gpt.com](https://docs.nano-gpt.com)
2. Sign up for an account
3. Navigate to API Keys section
4. Generate a new API key
5. Copy the key

### Step 3: Verify Configuration

List configured secrets:

```powershell
# API project
dotnet user-secrets list --project ObsidianAI.Api

# AppHost project
dotnet user-secrets list --project ObsidianAI.AppHost
```

Expected output for API project:
```
LLM:OpenRouter:ApiKey = sk-or-v1-xxxx... (or your LMStudio key)
```

### Step 4: Select Provider

Update `appsettings.json` or use environment variable:

```json
{
  "LLM": {
    "Provider": "OpenRouter"  // or "LMStudio" or "NanoGPT"
  }
}
```

Or via environment variable:
```powershell
$env:LLM__Provider = "OpenRouter"  # or "LMStudio" or "NanoGPT"
```

---

## Production Deployment

### Environment Variables

For production deployments, use environment variables instead of user secrets. The application follows the [ASP.NET Core configuration hierarchy](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/):

**Configuration Precedence (highest to lowest):**
1. Environment variables
2. User secrets (development only)
3. appsettings.{Environment}.json
4. appsettings.json

### Setting Environment Variables

#### Windows (PowerShell)

```powershell
# System-wide (requires admin)
[System.Environment]::SetEnvironmentVariable("LLM__OpenRouter__ApiKey", "sk-or-v1-xxxx", "Machine")

# User-level
[System.Environment]::SetEnvironmentVariable("LLM__OpenRouter__ApiKey", "sk-or-v1-xxxx", "User")

# Process-level (current session only)
$env:LLM__OpenRouter__ApiKey = "sk-or-v1-xxxx"
$env:LLM__Provider = "OpenRouter"
```

#### Linux/macOS (Bash)

```bash
# Add to ~/.bashrc or ~/.bash_profile
export LLM__OpenRouter__ApiKey="sk-or-v1-xxxx"
export LLM__Provider="OpenRouter"

# Reload configuration
source ~/.bashrc
```

#### systemd Service (Linux)

Create `/etc/systemd/system/obsidianai.service`:

```ini
[Unit]
Description=ObsidianAI API Service
After=network.target

[Service]
Type=notify
WorkingDirectory=/opt/obsidianai
ExecStart=/usr/bin/dotnet /opt/obsidianai/ObsidianAI.Api.dll
Environment="LLM__Provider=OpenRouter"
Environment="LLM__OpenRouter__ApiKey=sk-or-v1-xxxx"
Environment="LLM__OpenRouter__Endpoint=https://openrouter.ai/api/v1"
Environment="LLM__OpenRouter__Model=google/gemini-2.5-flash-lite-preview-09-2025"
EnvironmentFile=/etc/obsidianai/secrets.env
Restart=always
RestartSec=10
User=obsidianai
Group=obsidianai

[Install]
WantedBy=multi-user.target
```

Create `/etc/obsidianai/secrets.env`:
```env
LLM__OpenRouter__ApiKey=sk-or-v1-xxxx
```

Secure the secrets file:
```bash
sudo chmod 600 /etc/obsidianai/secrets.env
sudo chown obsidianai:obsidianai /etc/obsidianai/secrets.env
```

---

## Azure Deployment

### Option 1: Azure Key Vault (Recommended)

#### Prerequisites
- Azure subscription
- Azure Key Vault resource
- Managed Identity or Service Principal with Key Vault access

#### Setup Steps

1. **Create Key Vault:**
```bash
az keyvault create \
  --name obsidianai-kv \
  --resource-group obsidianai-rg \
  --location eastus
```

2. **Add Secrets:**
```bash
az keyvault secret set \
  --vault-name obsidianai-kv \
  --name LLM--OpenRouter--ApiKey \
  --value "sk-or-v1-xxxx"
```

3. **Configure Application:**

Add NuGet package:
```bash
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
```

Update `Program.cs`:
```csharp
var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction())
{
    var keyVaultEndpoint = builder.Configuration["KeyVault:Endpoint"];
    if (!string.IsNullOrEmpty(keyVaultEndpoint))
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultEndpoint),
            new DefaultAzureCredential());
    }
}
```

4. **Assign Managed Identity:**
```bash
# Enable system-assigned identity on App Service
az webapp identity assign \
  --name obsidianai-api \
  --resource-group obsidianai-rg

# Grant access to Key Vault
az keyvault set-policy \
  --name obsidianai-kv \
  --object-id <managed-identity-principal-id> \
  --secret-permissions get list
```

### Option 2: App Service Application Settings

1. **Navigate to Azure Portal**
2. **Go to your App Service → Configuration → Application Settings**
3. **Add the following settings:**

| Name | Value | Slot Setting |
|------|-------|--------------|
| `LLM__Provider` | `OpenRouter` | ☐ |
| `LLM__OpenRouter__ApiKey` | `sk-or-v1-xxxx` | ☑ |
| `LLM__OpenRouter__Endpoint` | `https://openrouter.ai/api/v1` | ☐ |
| `LLM__OpenRouter__Model` | `google/gemini-2.5-flash-lite-preview-09-2025` | ☐ |

**Important:** Check "Deployment slot setting" for sensitive values like API keys to prevent them from being swapped between slots.

4. **Restart the App Service**

### Option 3: Azure CLI Deployment

```bash
az webapp config appsettings set \
  --name obsidianai-api \
  --resource-group obsidianai-rg \
  --settings \
    LLM__Provider="OpenRouter" \
    LLM__OpenRouter__ApiKey="@Microsoft.KeyVault(SecretUri=https://obsidianai-kv.vault.azure.net/secrets/LLM--OpenRouter--ApiKey/)" \
    LLM__OpenRouter__Endpoint="https://openrouter.ai/api/v1" \
    LLM__OpenRouter__Model="google/gemini-2.5-flash-lite-preview-09-2025"
```

---

## Docker Deployment

### Using .env File (Development)

1. **Create `.env` file in repository root:**

```env
# LLM Configuration
LLM__Provider=OpenRouter
LLM__OpenRouter__Endpoint=https://openrouter.ai/api/v1
LLM__OpenRouter__ApiKey=sk-or-v1-xxxx
LLM__OpenRouter__Model=google/gemini-2.5-flash-lite-preview-09-2025

# LM Studio (alternative)
LLM__LMStudio__Endpoint=http://localhost:1234/v1
LLM__LMStudio__ApiKey=lm-studio
LLM__LMStudio__Model=openai/gpt-oss-20b

# MCP Gateway
MCP_ENDPOINT=http://mcp-gateway:8033/mcp
```

2. **Update `docker-compose.yml`:**

```yaml
services:
  api:
    image: obsidianai-api:latest
    environment:
      - LLM__Provider=${LLM__Provider}
      - LLM__OpenRouter__Endpoint=${LLM__OpenRouter__Endpoint}
      - LLM__OpenRouter__ApiKey=${LLM__OpenRouter__ApiKey}
      - LLM__OpenRouter__Model=${LLM__OpenRouter__Model}
    env_file:
      - .env
```

3. **Run with compose:**

```powershell
docker compose up -d
```

### Using Docker Secrets (Production)

1. **Create secret files:**

```powershell
# Create secrets directory
mkdir secrets

# Create secret file
"sk-or-v1-xxxx" | Out-File -FilePath secrets/openrouter_api_key -NoNewline
```

2. **Update `docker-compose.yml`:**

```yaml
version: '3.8'

services:
  api:
    image: obsidianai-api:latest
    secrets:
      - openrouter_api_key
    environment:
      - LLM__Provider=OpenRouter
      - LLM__OpenRouter__ApiKey_File=/run/secrets/openrouter_api_key
    command: >
      sh -c '
      export LLM__OpenRouter__ApiKey=$(cat /run/secrets/openrouter_api_key)
      dotnet ObsidianAI.Api.dll
      '

secrets:
  openrouter_api_key:
    file: ./secrets/openrouter_api_key
```

3. **Secure secrets directory:**

```bash
chmod 700 secrets
chmod 600 secrets/*
```

---

## Verification

### Application Startup Validation

The application validates required secrets on startup. If a secret is missing, you'll see:

```
Unhandled exception. System.InvalidOperationException: OpenRouter API key is not configured.
Set it via user secrets (dotnet user-secrets set "LLM:OpenRouter:ApiKey" "your-key")
or environment variable LLM__OpenRouter__ApiKey.
```

### Manual Testing

#### Test API Endpoint

```powershell
curl http://localhost:5095/api/llm/provider
```

Expected response:
```json
{
  "provider": "OpenRouter"
}
```

#### Test Health Endpoint

```powershell
curl http://localhost:5095/health
```

Expected response:
```json
{
  "status": "Healthy",
  "results": {
    "self": { "status": "Healthy" },
    "mcp": { "status": "Healthy" },
    "llm": {
      "status": "Healthy",
      "description": "LLM client ready for model 'google/gemini-2.5-flash-lite-preview-09-2025'"
    }
  }
}
```

---

## Troubleshooting

### Issue: "API key is not configured"

**Solution:**
1. Verify secrets are set correctly:
   ```powershell
   dotnet user-secrets list --project ObsidianAI.Api
   ```

2. Check for typos in secret keys (note the colons: `:`)

3. Ensure the correct provider is selected in `appsettings.json`

### Issue: "Unknown LLM provider"

**Solution:**
Update `appsettings.json` or set environment variable:
```powershell
$env:LLM__Provider = "OpenRouter"  # or "LMStudio" or "NanoGPT"
```

### Issue: User secrets not found

**Solution:**
Initialize user secrets for the project:
```powershell
dotnet user-secrets init --project ObsidianAI.Api
```

### Issue: LM Studio connection failed

**Solution:**
1. Verify LM Studio is running
2. Check endpoint is accessible:
   ```powershell
   curl http://localhost:1234/v1/models
   ```
3. Ensure model is loaded in LM Studio

### Issue: Docker container can't access user secrets

**Solution:**
User secrets are for development only. In Docker:
1. Use `.env` file for local Docker development
2. Use Docker secrets for production
3. Use environment variables in `docker-compose.yml`

---

## Security Best Practices

### ✅ Do's

- ✅ Use User Secrets for local development
- ✅ Use Azure Key Vault for production in Azure
- ✅ Use Docker secrets for containerized deployments
- ✅ Rotate API keys regularly
- ✅ Use different keys for different environments
- ✅ Enable audit logging for secret access
- ✅ Set minimal required permissions
- ✅ Use managed identities when possible

### ❌ Don'ts

- ❌ Never commit secrets to source control
- ❌ Never share secrets via email or chat
- ❌ Never log API keys
- ❌ Never hardcode secrets in application code
- ❌ Never store secrets in appsettings.json
- ❌ Never use production keys in development
- ❌ Never disable secret validation in production

---

## Additional Resources

- [ASP.NET Core User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets)
- [Azure Key Vault Configuration Provider](https://learn.microsoft.com/aspnet/core/security/key-vault-configuration)
- [Docker Secrets Documentation](https://docs.docker.com/engine/swarm/secrets/)
- [Environment Variables in .NET](https://learn.microsoft.com/dotnet/core/extensions/configuration-providers#environment-variable-configuration-provider)
- [OpenRouter API Documentation](https://openrouter.ai/docs)
- [LM Studio Documentation](https://lmstudio.ai/docs)

---

## Support

For issues or questions:
1. Check the [Troubleshooting](#troubleshooting) section
2. Review logs in `Logs/` directory
3. Open an issue on GitHub
4. Contact the development team

---

**Last Updated:** October 18, 2025  
**Next Review:** November 18, 2025
