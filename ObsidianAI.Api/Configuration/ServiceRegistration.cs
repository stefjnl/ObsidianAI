using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ObsidianAI.Api.HealthChecks;
using ObsidianAI.Api.Services;
using ObsidianAI.Application.DI;
using ObsidianAI.Application.Services;
using ObsidianAI.Infrastructure.Configuration;
using ObsidianAI.Infrastructure.DI;
using ObsidianAI.Infrastructure.HealthChecks;
using ObsidianAI.Infrastructure.LLM;
using System;

namespace ObsidianAI.Api.Configuration;

/// <summary>
/// Service registration helpers for the ObsidianAI API application.
/// </summary>
public static class ServiceRegistration
{
    /// <summary>
    /// Adds API, application, and infrastructure services required by ObsidianAI.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddObsidianApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AppSettings>(configuration);

        services.AddSingleton<McpClientService>();
        services.AddHostedService<McpClientService>();
        services.AddSingleton<IMcpClientProvider>(sp => sp.GetRequiredService<McpClientService>());
    services.AddSingleton<IMicrosoftLearnMcpClientProvider, MicrosoftLearnMcpClient>();

        services.AddObsidianAI(configuration);
        services.AddObsidianApplication();

        services.AddAntiforgery();

        services.AddSingleton<ILlmClientFactory>(sp =>
        {
            var appSettings = sp.GetRequiredService<IOptions<AppSettings>>();
            var provider = appSettings.Value.LLM.Provider?.Trim() ?? "LMStudio";

            return provider.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase)
                ? new OpenRouterClientFactory(appSettings)
                : new LmStudioClientFactory(appSettings);
        });

        services.AddHealthChecks()
            .AddCheck<McpHealthCheck>("mcp")
            .AddCheck<LlmHealthCheck>("llm")
            .AddCheck<MicrosoftLearnHealthCheck>("microsoft-learn");

        return services;
    }
}
