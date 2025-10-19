namespace ObsidianAI.Infrastructure.LLM;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ObsidianAI.Domain.Ports;
using ObsidianAI.Infrastructure.Configuration;
using ObsidianAI.Infrastructure.LLM.Clients;
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
        // HTTP Clients
        // ========================================================================
        
        // OpenRouter HTTP Client
        services.AddHttpClient<OpenRouterClient>((serviceProvider, client) =>
        {
            var settings = configuration
                .GetSection(OpenRouterSettings.SectionName)
                .Get<OpenRouterSettings>() ?? new OpenRouterSettings();
            
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        });

        // NanoGpt HTTP Client
        services.AddHttpClient<NanoGptClient>((serviceProvider, client) =>
        {
            var settings = configuration
                .GetSection(NanoGptSettings.SectionName)
                .Get<NanoGptSettings>() ?? new NanoGptSettings();
            
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        });

        // LMStudio HTTP Client
        services.AddHttpClient<LMStudioClient>((serviceProvider, client) =>
        {
            var settings = configuration
                .GetSection(LMStudioSettings.SectionName)
                .Get<LMStudioSettings>() ?? new LMStudioSettings();
            
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        });

        // ========================================================================
        // AI Clients (Infrastructure Layer)
        // ========================================================================
        
        services.AddScoped<IAIClient, OpenRouterClient>();
        services.AddScoped<IAIClient, NanoGptClient>();
        services.AddScoped<IAIClient, LMStudioClient>();

        // ========================================================================
        // Factory
        // ========================================================================
        
        services.AddScoped<IAIClientFactory, AIClientFactory>();

        return services;
    }
}