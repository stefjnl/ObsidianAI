using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.AI;

namespace ObsidianAI.Domain.Ports;

/// <summary>
/// Abstraction over AgentThread storage for managing multi-turn conversations.
/// </summary>
public interface IAgentThreadProvider
{
    /// <summary>
    /// Registers an AgentThread instance for later retrieval.
    /// </summary>
    /// <param name="thread">Thread instance to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A unique identifier that can be used to retrieve the thread later.</returns>
    Task<string> RegisterThreadAsync(AgentThread thread, CancellationToken ct = default);

    /// <summary>
    /// Retrieves an AgentThread by its identifier.
    /// </summary>
    /// <param name="threadId">Identifier returned from <see cref="RegisterThreadAsync"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<AgentThread?> GetThreadAsync(string threadId, CancellationToken ct = default);

    /// <summary>
    /// Deletes an AgentThread and releases any associated resources.
    /// </summary>
    /// <param name="threadId">Thread identifier to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteThreadAsync(string threadId, CancellationToken ct = default);
}
