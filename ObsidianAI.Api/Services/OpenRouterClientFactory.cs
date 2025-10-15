using Microsoft.Agents.AI;
using System;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using System.ClientModel;
using ObsidianAI.Infrastructure.Configuration;

namespace ObsidianAI.Api.Services
{
    /// <summary>
    /// Factory for creating an IChatClient backed by OpenRouter (OpenAI-compatible).
    /// Reads configuration from the "LLM:OpenRouter" section in appsettings.
    /// </summary>
    public sealed class OpenRouterClientFactory : ILlmClientFactory
    {
        private readonly OpenRouterSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenRouterClientFactory"/> class.
        /// </summary>
        /// <param name="appSettings">Application settings containing LLM configuration.</param>
        public OpenRouterClientFactory(IOptions<AppSettings> appSettings)
        {
            _settings = appSettings.Value.LLM.OpenRouter;
        }

        /// <inheritdoc />
        public IChatClient CreateChatClient()
        {
            var endpoint = _settings.Endpoint ?? "https://openrouter.ai/api/v1";
            var apiKey = _settings.ApiKey ?? throw new InvalidOperationException("OpenRouter API key missing");
            var model = _settings.Model ?? "google/gemini-2.5-flash-lite-preview-09-2025";

            var openAIClient = new OpenAIClient(
                new ApiKeyCredential(apiKey),
                new OpenAIClientOptions { Endpoint = new Uri(endpoint.Trim()) }
            );

            return openAIClient.GetChatClient(model).AsIChatClient();
        }

        /// <inheritdoc />
        public string GetModelName() => _settings.Model ?? "unknown";
    }
}