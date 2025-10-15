using Microsoft.Extensions.AI;

namespace ObsidianAI.Infrastructure.LLM;

/// <summary>
/// Factory abstraction for creating AI chat clients based on configured LLM provider.
/// </summary>
public interface ILlmClientFactory
{
    /// <summary>
    /// Creates an <see cref="IChatClient"/> for the configured provider and model.
    /// </summary>
    /// <returns>An <see cref="IChatClient"/> instance configured for the current LLM provider.</returns>
    IChatClient CreateChatClient();

    /// <summary>
    /// Gets the configured model identifier used by the chat client.
    /// </summary>
    /// <returns>The model name as configured in application settings.</returns>
    string GetModelName();
}
