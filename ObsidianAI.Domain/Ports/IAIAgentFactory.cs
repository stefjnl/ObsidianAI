namespace ObsidianAI.Domain.Ports
{


    /// <summary>
    /// Factory to build <see cref="IChatAgent"/> instances from provider configuration and instructions.
    /// </summary>
    public interface IAIAgentFactory
    {
        /// <summary>
        /// Gets the human-readable provider name represented by this factory.
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Creates a new <see cref="IChatAgent"/> configured with the specified instructions.
        /// </summary>
        /// <param name="instructions">System or agent instructions guiding the chat agent behavior.</param>
        /// <param name="tools">Optional collection of AI tools (e.g., from MCP) to provide to the agent.</param>
        /// <param name="threadProvider">Optional provider used to retrieve persistent agent thread state.</param>
        /// <param name="cancellationToken">A token used to cancel the agent creation process.</param>
        /// <returns>An initialized <see cref="IChatAgent"/> ready to process messages.</returns>
        Task<IChatAgent> CreateAgentAsync(
            string instructions,
            System.Collections.Generic.IEnumerable<object>? tools = null,
            IAgentThreadProvider? threadProvider = null,
            System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the default or configured model name used by the underlying provider.
        /// </summary>
        /// <returns>The model identifier string.</returns>
        string GetModelName();
    }
}