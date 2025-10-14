using Microsoft.Extensions.AI;

namespace ObsidianAI.Api.Services
{
    /// <summary>
    /// Factory abstraction for creating AI chat clients based on configured LLM provider.
    /// </summary>
    public interface ILlmClientFactory
    {
        /// <summary>
        /// Create an IChatClient for the configured provider and model.
        /// </summary>
        /// <returns>An IChatClient instance configured for the current LLM provider.</returns>
        IChatClient CreateChatClient();

        /// <summary>
        /// Gets the configured model identifier to be used by the chat client.
        /// </summary>
        /// <returns>The model name as configured in appsettings.</returns>
        string GetModelName();
    }
}