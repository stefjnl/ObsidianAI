namespace ObsidianAI.Domain.Ports
{
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
        /// <param name="ct">A cancellation token to cancel the operation.</param>
        /// <returns>The model response as a string.</returns>
        Task<string> SendAsync(string message, CancellationToken ct = default);

        /// <summary>
        /// Streams the model output as an asynchronous sequence of events.
        /// </summary>
        /// <param name="message">The user message to send.</param>
        /// <param name="ct">A cancellation token to cancel the operation.</param>
        /// <returns>An asynchronous stream of <see cref="ChatStreamEvent"/> items.</returns>
        IAsyncEnumerable<ChatStreamEvent> StreamAsync(string message, CancellationToken ct = default);
    }
}