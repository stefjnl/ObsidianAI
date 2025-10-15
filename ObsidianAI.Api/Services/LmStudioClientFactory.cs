using Microsoft.Agents.AI;
using System;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using OpenAI;
using System.ClientModel;
using ObsidianAI.Api.Models;

namespace ObsidianAI.Api.Services
{
    /// <summary>
    /// Factory for creating an IChatClient backed by LM Studio (OpenAI-compatible).
    /// Reads configuration from the "LLM:LMStudio" section in appsettings.
    /// </summary>
    public sealed class LmStudioClientFactory : ILlmClientFactory
    {
        private readonly LmStudioSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="LmStudioClientFactory"/> class.
        /// </summary>
        /// <param name="appSettings">Application settings containing LLM configuration.</param>
        public LmStudioClientFactory(IOptions<AppSettings> appSettings)
        {
            _settings = appSettings.Value.LLM.LMStudio;
        }

        /// <inheritdoc />
        public IChatClient CreateChatClient()
        {
            var endpoint = _settings.Endpoint ?? "http://localhost:1234/v1";
            var apiKey = _settings.ApiKey ?? "lm-studio";
            var model = _settings.Model ?? "openai/gpt-oss-20b";

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