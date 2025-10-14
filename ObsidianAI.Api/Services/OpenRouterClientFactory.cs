using Microsoft.Agents.AI;
using System;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using System.ClientModel;

namespace ObsidianAI.Api.Services
{
    /// <summary>
    /// Factory for creating an IChatClient backed by OpenRouter (OpenAI-compatible).
    /// Reads configuration from the "LLM:OpenRouter" section in appsettings.
    /// </summary>
    public sealed class OpenRouterClientFactory : ILlmClientFactory
    {
        private readonly IConfiguration _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenRouterClientFactory"/> class.
        /// </summary>
        /// <param name="configuration">Application configuration root.</param>
        public OpenRouterClientFactory(IConfiguration configuration)
        {
            _config = configuration.GetSection("LLM:OpenRouter");
        }

        /// <inheritdoc />
        public IChatClient CreateChatClient()
        {
            var endpoint = _config["Endpoint"] ?? "https://openrouter.ai/api/v1";
            var apiKey = _config["ApiKey"] ?? throw new InvalidOperationException("OpenRouter API key missing");
            var model = _config["Model"] ?? "google/gemini-2.5-flash-lite-preview-09-2025";

            var openAIClient = new OpenAIClient(
                new ApiKeyCredential(apiKey),
                new OpenAIClientOptions { Endpoint = new Uri(endpoint.Trim()) }
            );

            return openAIClient.GetChatClient(model).AsIChatClient();
        }

        /// <inheritdoc />
        public string GetModelName() => _config["Model"] ?? "unknown";
    }
}