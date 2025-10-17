using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ObsidianAI.Domain.Models;

namespace ObsidianAI.Domain.Services;

/// <summary>
/// Service for reflecting on file operations to determine safety and approval requirements.
/// </summary>
public interface IReflectionService
{
    /// <summary>
    /// Analyzes a file operation for safety and determines if it should be approved, rejected, or requires user confirmation.
    /// </summary>
    /// <param name="toolName">The name of the tool being invoked.</param>
    /// <param name="arguments">The arguments passed to the tool.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The reflection result indicating approval status and reasoning.</returns>
    Task<ReflectionResult> ReflectAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default);
}