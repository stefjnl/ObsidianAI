# Connecting ObsidianAI to Your Vault: A Comprehensive Guide

This guide details exactly how the ObsidianAI system connects to your local Obsidian vault using the Model Context Protocol (MCP) in a Dockerized environment.

---

## 1. High-Level Architecture

The connection is not direct. It involves a chain of delegated services to ensure security and isolation:

```mermaid
graph TD
    A[ObsidianAI Web App] -->|Use Tool| B[MCP Client]
    B -->|HTTP/SSE| C[MCP Gateway]
    C -->|Stdio| D[Obsidian MCP Server]
    D -->|REST API| E[Obsidian App (Localhost)]
    E -->|File IO| F[Vault Files]
```

1.  **Web App (Docker)**: The brain. It decides *what* to do (e.g., "Read file X").
2.  **MCP Gateway (Docker)**: A bridge that exposes the MCP server over HTTP.
3.  **Obsidian MCP Server (Docker Container)**: The tool executor. It runs inside the container but talks to the host.
4.  **Obsidian REST API (Host Machine)**: The actual plugin running inside your Obsidian desktop app.
5.  **Vault**: Your physical files.

---

## 2. Prerequisites & Configuration

### A. The Obsidian Desktop App
You must have the **Local REST API** plugin installed and configured in Obsidian.
1.  **Install**: Community Plugins -> Browse -> "Local REST API".
2.  **Enable**: Toggle it on.
3.  **Configure**:
    *   **Port**: `27124` (Recommended)
    *   **Protocol**: HTTPS (Recommended)
    *   **API Key**: Copy this key. You will need it.

### B. Docker Environment (Host)
The `docker-compose.yml` file must correctly map the host's network so the container can "see" the Obsidian app.

**Key Settings:**
*   `extra_hosts`:
    ```yaml
    extra_hosts:
      - "host.docker.internal:host-gateway"
    ```
    *Why?* This allows the container to resolve `host.docker.internal` to your actual machine's IP address.

### C. MCP Server Configuration
The MCP server needs to know where the Obsidian REST API is. This is defined in `C:\Users\<User>\.docker\mcp\config.yaml`:

```yaml
servers:
  obsidian:
    env:
      OBSIDIAN_API_KEY: "YOUR_COPIED_API_KEY"
      OBSIDIAN_HOST: "host.docker.internal"
      OBSIDIAN_PORT: "27124"
      OBSIDIAN_HTTPS: "true"
```

---

## 3. The Connection Chain (Detailed)

When you ask the AI to "List files":

1.  **Blazor (Container)**:
    *   Calls `McpClientService.ListToolsAsync()`.
    *   Uses `MCP_ENDPOINT` env var: `http://host.docker.internal:8033/mcp`.
    *   Authenticates using `MCP_GATEWAY_AUTH_TOKEN`.

2.  **MCP Gateway (Host/Container)**:
    *   Receives the request on port `8033`.
    *   Forwards it to the `obsidian` server defined in `registry.yaml` and `config.yaml`.

3.  **Obsidian MCP Server**:
    *   Receives the command.
    *   Uses the `OBSIDIAN_HOST` and `OBSIDIAN_API_KEY` env vars to make a standard HTTPS request.
    *   URL: `https://host.docker.internal:27124/vault/`

4.  **Obsidian App**:
    *   Receives the HTTPS request.
    *   Returns the JSON list of files from your active vault.

---

## 4. Troubleshooting Common Issues

### "Connection Refused" (Container -> Host)
*   **Symptom**: The app starts, but clicking "Vault" shows nothing or logs an error.
*   **Fix**: Ensure `extra_hosts` is present in `docker-compose.yml`. Ensure Obsidian is actually running.

### "Connection Refused" (Internal Loopback)
*   **Symptom**: Browser shows `SocketException`, chat doesn't load.
*   **Fix**: This describes the Blazor app trying to talk to itself.
    *   **Incorrect**: `localhost:5244` (External port, unreachable from inside).
    *   **Correct**: `localhost:8080` (Internal port).
    *   **Verified Fix**: We set `ApiBaseUrl: http://localhost:8080` in `docker-compose.yml`.

### authentication Failed (401/403)
*   **Symptom**: MCP logs show unauthorized.
*   **Fix**:
    1.  Check `OBSIDIAN_API_KEY` in `config.yaml` matches the one in the Obsidian plugin.
    2.  Check `MCP_GATEWAY_AUTH_TOKEN` in `.env` matches the one expected by the gateway.

---

## 5. Verification Checklist

- [ ] Obsidian App is running.
- [ ] Local REST API plugin is enabled (Port 27124).
- [ ] `docker-compose.yml` has `extra_hosts`.
- [ ] `config.yaml` has correct API Key.
- [ ] `ApiBaseUrl` is set to `http://localhost:8080`.
- [ ] Browser can access `http://localhost:5244`.
