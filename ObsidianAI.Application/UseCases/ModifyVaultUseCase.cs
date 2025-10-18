using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ObsidianAI.Application.Services;
using ObsidianAI.Domain.Models;
using ObsidianAI.Domain.Ports;
using ObsidianAI.Domain.Services;

namespace ObsidianAI.Application.UseCases;

/// <summary>
/// Use case for modifying vault files through various operations.
/// </summary>
public class ModifyVaultUseCase
{
    private static readonly IReadOnlyDictionary<string, string> PatchOperationMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["modify"] = "replace",
        ["replace"] = "replace",
        ["write"] = "replace",
        ["update"] = "replace",
        ["overwrite"] = "replace",
        ["patch"] = "patch",
        ["insert"] = "insert",
        ["prepend"] = "prepend"
    };

    private readonly IVaultToolExecutor _executor;
    private readonly IVaultPathNormalizer _normalizer;
    private readonly IVaultIndexCache? _vaultIndexCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModifyVaultUseCase"/> class.
    /// </summary>
    /// <param name="executor">Executor for vault tool operations.</param>
    /// <param name="normalizer">Normalizer for vault paths.</param>
    public ModifyVaultUseCase(IVaultToolExecutor executor, IVaultPathNormalizer normalizer, IVaultIndexCache? vaultIndexCache = null)
    {
        _executor = executor;
        _normalizer = normalizer;
        _vaultIndexCache = vaultIndexCache;
    }

    /// <summary>
    /// Executes the modify vault use case.
    /// </summary>
    /// <param name="operation">The operation to perform (append, modify, patch, write, delete, create).</param>
    /// <param name="filePath">The file path to operate on.</param>
    /// <param name="content">The content for the operation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<OperationResult> ExecuteAsync(string operation, string filePath, string content, CancellationToken ct = default)
    {
        var normalizedPath = _normalizer.Normalize(filePath);

        var lowerOperation = operation.ToLowerInvariant();
        OperationResult result;

        if (lowerOperation.Equals("append", StringComparison.Ordinal))
        {
            result = await _executor.AppendAsync(normalizedPath, content, ct).ConfigureAwait(false);
        }
        else if (lowerOperation.Equals("delete", StringComparison.Ordinal))
        {
            result = await _executor.DeleteAsync(normalizedPath, ct).ConfigureAwait(false);
        }
        else if (lowerOperation.Equals("create", StringComparison.Ordinal))
        {
            result = await _executor.CreateAsync(normalizedPath, content, ct).ConfigureAwait(false);
        }
        else if (PatchOperationMap.TryGetValue(lowerOperation, out var patchMode))
        {
            result = await _executor.PatchAsync(normalizedPath, content, patchMode, ct).ConfigureAwait(false);
        }
        else
        {
            return new OperationResult(false, $"Unsupported operation: {operation}", normalizedPath);
        }

        if (result.Success)
        {
            _vaultIndexCache?.InvalidateAll();
        }

        return result;
    }
}