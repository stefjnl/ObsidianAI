using ObsidianAI.Domain.Models;
using ObsidianAI.Domain.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace ObsidianAI.Infrastructure.Vault;

/// <summary>
/// A null implementation of IVaultToolExecutor that returns failure results when MCP is unavailable.
/// This allows the application to continue running without MCP functionality.
/// </summary>
public class NullVaultToolExecutor : IVaultToolExecutor
{
    /// <inheritdoc/>
    public Task<OperationResult> AppendAsync(string filePath, string content, CancellationToken ct = default)
    {
        return Task.FromResult(new OperationResult(false, "MCP server is not available. Vault operations are disabled.", filePath));
    }

    /// <inheritdoc/>
    public Task<OperationResult> PatchAsync(string filePath, string content, string operation, CancellationToken ct = default)
    {
        return Task.FromResult(new OperationResult(false, "MCP server is not available. Vault operations are disabled.", filePath));
    }

    /// <inheritdoc/>
    public Task<OperationResult> DeleteAsync(string filePath, CancellationToken ct = default)
    {
        return Task.FromResult(new OperationResult(false, "MCP server is not available. Vault operations are disabled.", filePath));
    }

    /// <inheritdoc/>
    public Task<OperationResult> CreateAsync(string filePath, string content, CancellationToken ct = default)
    {
        return Task.FromResult(new OperationResult(false, "MCP server is not available. Vault operations are disabled.", filePath));
    }
}