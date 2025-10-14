using Microsoft.Agents.AI;
using System;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using System.ClientModel;

namespace ObsidianAI.Api.Services
{
    /// <summary>
    /// Factory for creating an IChatClient backed by LM Studio (OpenAI-compatible).
    /// Reads configuration from the "LLM:LMStudio" section in appsettings.
    /// </summary>
    public sealed class LmStudioClientFactory : ILlmClientFactory
    {
        private readonly IConfiguration _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="LmStudioClientFactory"/> class.
        /// </summary>
        /// <param name="configuration">Application configuration root.</param>
        public LmStudioClientFactory(IConfiguration configuration)
        {
            _config = configuration.GetSection("LLM:LMStudio");
        }

        /// <inheritdoc />
        public IChatClient CreateChatClient()
        {
            var endpoint = _config["Endpoint"] ?? "http://localhost:1234/v1";
            var apiKey = _config["ApiKey"] ?? "lm-studio";
            var model = _config["Model"] ?? "openai/gpt-oss-20b";

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