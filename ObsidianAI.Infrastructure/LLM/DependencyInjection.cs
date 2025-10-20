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
        // ========================================================================
        // Configuration (Infrastructure Layer - Provider Settings)
        // ========================================================================
        
        services.Configure<OpenRouterSettings>(
            configuration.GetSection(OpenRouterSettings.SectionName));

        services.Configure<NanoGptSettings>(
            configuration.GetSection(NanoGptSettings.SectionName));

        services.Configure<LMStudioSettings>(
            configuration.GetSection(LMStudioSettings.SectionName));

        // ========================================================================
        // ChatAgent Concrete Registrations (serve both IChatAgent and IAIClient)
        // ========================================================================
        
        // Register concrete agent types (scoped per request)
        services.AddScoped<OpenRouterChatAgent>();
        services.AddScoped<LmStudioChatAgent>();
        services.AddScoped<NanoGptChatAgent>();

        // Register keyed IChatAgent services (for agent-based workflows)
        services.AddKeyedScoped<IChatAgent, OpenRouterChatAgent>("OpenRouter", (sp, key) => sp.GetRequiredService<OpenRouterChatAgent>());
        services.AddKeyedScoped<IChatAgent, LmStudioChatAgent>("LMStudio", (sp, key) => sp.GetRequiredService<LmStudioChatAgent>());
        services.AddKeyedScoped<IChatAgent, NanoGptChatAgent>("NanoGPT", (sp, key) => sp.GetRequiredService<NanoGptChatAgent>());

        // ========================================================================
        // Factory
        // ========================================================================
        
        services.AddScoped<IAIClientFactory, AIClientFactory>();

        return services;
    }
}