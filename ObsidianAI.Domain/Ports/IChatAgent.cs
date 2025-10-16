namespace ObsidianAI.Domain.Ports
{
    using Microsoft.Agents.AI;
    using ObsidianAI.Domain.Models;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provider-agnostic chat agent abstraction.
    /// </summary>
    public interface IChatAgent
    {
        /// <summary>
        /// Sends a single message and returns the full string response.
        /// </summary>
        /// <param name="message">The user message to send.</param>
    /// <param name="threadId">Optional agent thread identifier used to preserve conversation state.</param>
        /// <param name="ct">A cancellation token to cancel the operation.</param>
        /// <returns>The model response as a string.</returns>
        Task<string> SendAsync(string message, string? threadId = null, CancellationToken ct = default);

        /// <summary>
        /// Streams the model output as an asynchronous sequence of events.
        /// </summary>
        /// <param name="message">The user message to send.</param>
    /// <param name="threadId">Optional agent thread identifier used to preserve conversation state.</param>
        /// <param name="ct">A cancellation token to cancel the operation.</param>
        /// <returns>An asynchronous stream of <see cref="ChatStreamEvent"/> items.</returns>
        IAsyncEnumerable<ChatStreamEvent> StreamAsync(string message, string? threadId = null, CancellationToken ct = default);

        /// <summary>
        /// Creates a new agent thread that can be reused for subsequent turns.
        /// </summary>
        /// <param name="ct">A cancellation token to cancel the operation.</param>
        /// <returns>The newly created <see cref="AgentThread"/>.</returns>
        Task<AgentThread> CreateThreadAsync(CancellationToken ct = default);
    }
}