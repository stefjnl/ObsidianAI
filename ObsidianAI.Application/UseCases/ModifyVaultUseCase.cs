namespace ObsidianAI.Application.UseCases;

/// <summary>
/// Use case for modifying vault files through various operations.
/// </summary>
public class ModifyVaultUseCase
{
    private readonly ObsidianAI.Domain.Ports.IVaultToolExecutor _executor;
    private readonly ObsidianAI.Domain.Services.IVaultPathNormalizer _normalizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModifyVaultUseCase"/> class.
    /// </summary>
    /// <param name="executor">Executor for vault tool operations.</param>
    /// <param name="normalizer">Normalizer for vault paths.</param>
    public ModifyVaultUseCase(ObsidianAI.Domain.Ports.IVaultToolExecutor executor, ObsidianAI.Domain.Services.IVaultPathNormalizer normalizer)
    {
        _executor = executor;
        _normalizer = normalizer;
    }

    /// <summary>
    /// Executes the modify vault use case.
    /// </summary>
    /// <param name="operation">The operation to perform (append, modify, patch, write, delete, create).</param>
    /// <param name="filePath">The file path to operate on.</param>
    /// <param name="content">The content for the operation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<ObsidianAI.Domain.Models.OperationResult> ExecuteAsync(string operation, string filePath, string content, CancellationToken ct = default)
    {
        var normalizedPath = _normalizer.Normalize(filePath);

        var lowerOperation = operation.ToLowerInvariant();
        return lowerOperation switch
        {
            "append" => await _executor.AppendAsync(normalizedPath, content, ct).ConfigureAwait(false),
            "modify" or "patch" or "write" => await _executor.PatchAsync(normalizedPath, content, "append", ct).ConfigureAwait(false),
            "delete" => await _executor.DeleteAsync(normalizedPath, ct).ConfigureAwait(false),
            "create" => await _executor.CreateAsync(normalizedPath, content, ct).ConfigureAwait(false),
            _ => new ObsidianAI.Domain.Models.OperationResult(false, $"Unsupported operation: {operation}", normalizedPath)
        };
    }
}