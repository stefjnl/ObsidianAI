namespace ObsidianAI.Infrastructure.LLM;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ObsidianAI.Domain.Ports;
using ObsidianAI.Infrastructure.Configuration;
using ObsidianAI.Infrastructure.LLM.Factories;

public static class DependencyInjection
{
    public static IServiceCollection AddAIProviderInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuration - NanoGPT only
        services.Configure<NanoGptSettings>(
            configuration.GetSection(NanoGptSettings.SectionName));

        // Register NanoGPT agent (serves both IChatAgent and IAIClient)
        services.AddScoped<NanoGptChatAgent>();

        // Register keyed IChatAgent service
        services.AddKeyedScoped<IChatAgent, NanoGptChatAgent>("NanoGPT", (sp, key) => sp.GetRequiredService<NanoGptChatAgent>());

        // Factory
        services.AddScoped<IAIClientFactory, AIClientFactory>();

        return services;
    }
}