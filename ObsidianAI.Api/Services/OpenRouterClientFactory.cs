using System;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using System.ClientModel;
using ObsidianAI.Infrastructure.Configuration;
using ObsidianAI.Infrastructure.LLM;

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
            var endpoint = _settings.Endpoint?.Trim();
            if (string.IsNullOrEmpty(endpoint))
            {
                throw new InvalidOperationException("OpenRouter endpoint not configured");
            }

            var apiKey = _settings.ApiKey?.Trim();
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("OpenRouter API key missing");
            }

            var model = _settings.Model?.Trim();
            if (string.IsNullOrEmpty(model))
            {
                throw new InvalidOperationException("OpenRouter model not configured");
            }

            var openAIClient = new OpenAIClient(
                new ApiKeyCredential(apiKey),
                new OpenAIClientOptions { Endpoint = new Uri(endpoint) }
            );

            return openAIClient.GetChatClient(model).AsIChatClient();
        }

        /// <inheritdoc />
    public string GetModelName() => _settings.Model?.Trim() ?? string.Empty;
    }
}